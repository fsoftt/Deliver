using Deliver.Contracts.Billing;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;

namespace Deliver.Shipping.Application.Features.Billing;

// Shipping keeps a copy of the facts Billing publishes so that GET /shipments/{id} can show them.
// Until those events arrive the fields are empty. That is intentional eventual consistency.

public sealed class DeliveryPriceCalculatedHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<DeliveryPriceCalculatedIntegrationEvent>
{
    public async Task HandleAsync(DeliveryPriceCalculatedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetAsync(new ShipmentId(integrationEvent.ShipmentId), cancellationToken)
            ?? throw new NotFoundException("Shipment", integrationEvent.ShipmentId);

        shipment.QuotePrice(new Money(integrationEvent.Amount, integrationEvent.Currency));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Last step of the delivery saga: Delivered → PaymentCaptured.</summary>
public sealed class PaymentCapturedHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<PaymentCapturedIntegrationEvent>
{
    public async Task HandleAsync(PaymentCapturedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetAsync(new ShipmentId(integrationEvent.ShipmentId), cancellationToken)
            ?? throw new NotFoundException("Shipment", integrationEvent.ShipmentId);

        shipment.RecordPaymentCaptured();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Failure branch of the delivery saga: Delivered → PaymentFailed → ShipmentDeliveryPaymentFailed.</summary>
public sealed class PaymentFailedHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
    : IIntegrationEventHandler<PaymentFailedIntegrationEvent>
{
    public async Task HandleAsync(PaymentFailedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetAsync(new ShipmentId(integrationEvent.ShipmentId), cancellationToken)
            ?? throw new NotFoundException("Shipment", integrationEvent.ShipmentId);

        shipment.RecordPaymentFailed(integrationEvent.Reason, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
