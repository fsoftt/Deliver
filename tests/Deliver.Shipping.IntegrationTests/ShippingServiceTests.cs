using System.Net;
using System.Net.Http.Json;
using Deliver.Contracts.Billing;
using Deliver.Contracts.Dispatch;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.Messaging.Inbox;
using Deliver.Messaging.Outbox;
using Deliver.Shipping.Application.Features.GetShipment;
using Deliver.Shipping.Infrastructure.Persistence;
using Deliver.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(InfrastructureFixture))]

namespace Deliver.Shipping.IntegrationTests;

/// <summary>
/// The Shipping service end to end: HTTP → domain → PostgreSQL → outbox → RabbitMQ, and
/// RabbitMQ → idempotent consumer → domain → PostgreSQL.
/// </summary>
public sealed class ShippingServiceTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private ServiceFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private TestBroker _broker = null!;

    public async ValueTask InitializeAsync()
    {
        _factory = new ServiceFactory<Program>(infrastructure, "ShippingDb", "shipping");
        _client = _factory.CreateClient();
        _broker = await TestBroker.ConnectAsync(infrastructure.RabbitMqConnectionString);
        await _broker.WaitForQueueAsync(Infrastructure.DependencyInjection.QueueName);
    }

    [Fact]
    public async Task Creating_a_shipment_stores_it_and_publishes_ShipmentCreated_through_the_outbox()
    {
        var observer = await _broker.ObserveAsync(ShipmentCreatedIntegrationEvent.Exchange, ShipmentCreatedIntegrationEvent.RoutingKey);
        var correlationId = Guid.NewGuid().ToString();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/shipments") { Content = JsonContent.Create(NewShipmentRequest()) };
        request.Headers.Add(CorrelationContext.HeaderName, correlationId);
        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var shipmentId = (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var message = await _broker.ReceiveAsync(observer);
        message.Envelope!.EventType.ShouldBe("ShipmentCreated");
        message.Envelope.CorrelationId.ShouldBe(correlationId);
        message.Envelope.Payload.GetProperty("shipmentId").GetGuid().ShouldBe(shipmentId);
        message.Envelope.Payload.GetProperty("totalWeightKg").GetDecimal().ShouldBe(5.25m);
        message.Header(MessagingDiagnostics.TraceParentHeader).ShouldNotBeNullOrEmpty();

        var shipment = await _client.GetFromJsonAsync<ShipmentDetails>($"/api/shipments/{shipmentId}");
        shipment!.Status.ShouldBe("Created");
        shipment.Price.ShouldBeNull(); // Billing has not answered yet: eventual consistency
    }

    [Fact]
    public async Task Invalid_input_is_rejected_with_400()
    {
        var response = await _client.PostAsJsonAsync("/api/shipments", NewShipmentRequest() with { Items = [] });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_shipment_cannot_be_picked_up_before_a_driver_is_assigned()
    {
        var shipmentId = await CreateShipmentAsync();

        var response = await _client.PostAsync($"/api/shipments/{shipmentId}/pickup", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_duplicated_DriverAssigned_event_assigns_the_driver_exactly_once()
    {
        var shipmentId = await CreateShipmentAsync();
        var driverAssigned = new DriverAssignedIntegrationEvent(Guid.NewGuid(), shipmentId, Guid.NewGuid(), "Carlos", DateTimeOffset.UtcNow);
        var messageId = Guid.NewGuid();

        await _broker.PublishAsync(driverAssigned, messageId);
        await _broker.PublishAsync(driverAssigned, messageId); // the broker redelivers

        var shipment = await Eventually.GetAsync(
            () => _client.GetFromJsonAsync<ShipmentDetails>($"/api/shipments/{shipmentId}"),
            s => s.Status == "Assigned");
        shipment.DriverId.ShouldBe(driverAssigned.DriverId);

        await Task.Delay(TimeSpan.FromSeconds(1));
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
        (await db.Set<ProcessedMessage>().CountAsync(m => m.MessageId == messageId)).ShouldBe(1);

        var assignedEvents = await db.Set<OutboxMessage>().Where(m => m.RoutingKey == "shipment.assigned").ToListAsync();
        assignedEvents.Count(m => m.Payload.Contains(shipmentId.ToString())).ShouldBe(1);
    }

    [Fact]
    public async Task The_price_calculated_by_Billing_eventually_shows_up_on_the_shipment()
    {
        var shipmentId = await CreateShipmentAsync();

        await _broker.PublishAsync(new DeliveryPriceCalculatedIntegrationEvent(shipmentId, Guid.NewGuid(), 12500, "CLP", DateTimeOffset.UtcNow));

        var shipment = await Eventually.GetAsync(
            () => _client.GetFromJsonAsync<ShipmentDetails>($"/api/shipments/{shipmentId}"),
            s => s.Price is not null);
        shipment.Price.ShouldBe(12500);
        shipment.Currency.ShouldBe("CLP");
    }

    private async Task<Guid> CreateShipmentAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/shipments", NewShipmentRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static CreateShipmentRequest NewShipmentRequest() => new(
        Guid.NewGuid(),
        new AddressRequest("Av. Providencia 1234", "Santiago", "7500000"),
        new AddressRequest("Calle Valparaíso 55", "Viña del Mar", "2520000"),
        [new ItemRequest("Books", 2, 1.5m), new ItemRequest("Laptop", 1, 2.25m)]);

    private sealed record CreateShipmentRequest(Guid CustomerId, AddressRequest PickupAddress, AddressRequest DeliveryAddress, List<ItemRequest> Items);

    private sealed record AddressRequest(string Street, string City, string PostalCode);

    private sealed record ItemRequest(string Description, int Quantity, decimal UnitWeightKg);

    private sealed record CreatedResponse(Guid Id);

    public async ValueTask DisposeAsync()
    {
        await _broker.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
