using Deliver.Billing.Domain.Invoices;
using Deliver.Billing.Domain.Pricing;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.SharedKernel;

namespace Deliver.Billing.Application.Features.Pricing;

/// <summary>
/// Prices the delivery as soon as the shipment exists. Shipping never waits for this: if Billing is down
/// the shipment is still created and the price shows up later (eventual consistency).
/// </summary>
public sealed class ShipmentCreatedHandler(IInvoiceRepository invoices, IUnitOfWork unitOfWork, TimeProvider clock)
    : IIntegrationEventHandler<ShipmentCreatedIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCreatedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        if (await invoices.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken) is not null)
            return;

        var price = DeliveryPricing.Calculate(integrationEvent.TotalWeightKg, integrationEvent.ItemCount, Tariff.Standard);
        invoices.Add(Invoice.Open(integrationEvent.ShipmentId, integrationEvent.CustomerId, price, clock.GetUtcNow()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Compensation: a cancelled shipment is never charged.</summary>
public sealed class ShipmentCancelledHandler(IInvoiceRepository invoices, IUnitOfWork unitOfWork, TimeProvider clock)
    : IIntegrationEventHandler<ShipmentCancelledIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCancelledIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken)
            ?? throw new NotFoundException("Invoice for shipment", integrationEvent.ShipmentId);

        invoice.Void(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
