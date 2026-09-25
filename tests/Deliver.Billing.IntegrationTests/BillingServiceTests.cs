using System.Net.Http.Json;
using Deliver.Billing.Application.Features.GetInvoice;
using Deliver.Billing.Infrastructure.Persistence;
using Deliver.Contracts.Billing;
using Deliver.Contracts.Shipping;
using Deliver.Messaging.Inbox;
using Deliver.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(InfrastructureFixture))]

namespace Deliver.Billing.IntegrationTests;

/// <summary>Billing reacting to shipment events: pricing, charging exactly once, and compensation.</summary>
public sealed class BillingServiceTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private static readonly AddressDto Pickup = new("Av. Providencia 1234", "Santiago", "7500000");
    private static readonly AddressDto Destination = new("Calle Valparaíso 55", "Viña del Mar", "2520000");

    private ServiceFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private TestBroker _broker = null!;

    public async ValueTask InitializeAsync()
    {
        _factory = new ServiceFactory<Program>(infrastructure, "BillingDb", "billing");
        _client = _factory.CreateClient();
        _broker = await TestBroker.ConnectAsync(infrastructure.RabbitMqConnectionString);
        await _broker.WaitForQueueAsync(Infrastructure.DependencyInjection.QueueName);
    }

    [Fact]
    public async Task A_delivered_shipment_is_charged_exactly_once_even_if_the_event_is_duplicated()
    {
        var (shipmentId, customerId) = await CreateShipmentAsync(weightKg: 5.25m, items: 3);
        var observer = await _broker.ObserveAsync(PaymentCapturedIntegrationEvent.Exchange, PaymentCapturedIntegrationEvent.RoutingKey);
        var delivered = new ShipmentDeliveredIntegrationEvent(shipmentId, customerId, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var messageId = Guid.NewGuid();

        await _broker.PublishAsync(delivered, messageId);
        await _broker.PublishAsync(delivered, messageId);   // redelivery of the same message
        await _broker.PublishAsync(delivered);              // same fact published twice by a buggy producer

        var invoice = await Eventually.GetAsync(() => GetInvoiceAsync(shipmentId), i => i.Status == "Paid");
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        invoice = (await GetInvoiceAsync(shipmentId))!;

        invoice.Amount.ShouldBe(14000);
        invoice.Payments.Count.ShouldBe(1);
        invoice.Payments[0].Succeeded.ShouldBeTrue();
        (await _broker.ReceiveAsync(observer)).Envelope!.Payload.GetProperty("shipmentId").GetGuid().ShouldBe(shipmentId);
        (await _broker.CountAsync(observer)).ShouldBe(0u);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        (await db.Set<ProcessedMessage>().CountAsync(m => m.MessageId == messageId)).ShouldBe(1);
    }

    [Fact]
    public async Task A_declined_payment_publishes_PaymentFailed()
    {
        // 100 kg → 4000 + 150000 = 154000 CLP, above the fake gateway's card limit.
        var (shipmentId, customerId) = await CreateShipmentAsync(weightKg: 100, items: 1);
        var observer = await _broker.ObserveAsync(PaymentFailedIntegrationEvent.Exchange, PaymentFailedIntegrationEvent.RoutingKey);

        await _broker.PublishAsync(new ShipmentDeliveredIntegrationEvent(shipmentId, customerId, Guid.NewGuid(), DateTimeOffset.UtcNow));

        var failed = await _broker.ReceiveAsync(observer);
        failed.Envelope!.Payload.GetProperty("shipmentId").GetGuid().ShouldBe(shipmentId);
        (await GetInvoiceAsync(shipmentId))!.Status.ShouldBe("PaymentFailed");
    }

    [Fact]
    public async Task A_cancelled_shipment_voids_its_invoice()
    {
        var (shipmentId, customerId) = await CreateShipmentAsync(weightKg: 1, items: 1);

        await _broker.PublishAsync(new ShipmentCancelledIntegrationEvent(shipmentId, customerId, null, "Customer request", DateTimeOffset.UtcNow));

        await Eventually.GetAsync(() => GetInvoiceAsync(shipmentId), i => i.Status == "Voided");
    }

    private async Task<(Guid ShipmentId, Guid CustomerId)> CreateShipmentAsync(decimal weightKg, int items)
    {
        var shipmentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        await _broker.PublishAsync(new ShipmentCreatedIntegrationEvent(shipmentId, customerId, Pickup, Destination, weightKg, items, DateTimeOffset.UtcNow));
        await Eventually.GetAsync(() => GetInvoiceAsync(shipmentId));
        return (shipmentId, customerId);
    }

    private async Task<InvoiceDetails?> GetInvoiceAsync(Guid shipmentId)
    {
        var response = await _client.GetAsync($"/api/billing/shipments/{shipmentId}/invoice");
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<InvoiceDetails>() : null;
    }

    public async ValueTask DisposeAsync()
    {
        await _broker.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
