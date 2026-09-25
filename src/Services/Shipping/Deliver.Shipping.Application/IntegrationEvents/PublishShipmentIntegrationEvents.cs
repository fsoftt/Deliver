using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;

namespace Deliver.Shipping.Application.IntegrationEvents;

/// <summary>
/// The boundary between the inside and the outside of the Shipping context: translates internal domain
/// events into public integration contracts and stores them in the outbox. Domain types never leak
/// onto the wire, so the domain model can evolve without breaking other services.
/// </summary>
public sealed class PublishShipmentIntegrationEvents(IOutbox outbox) :
    IDomainEventHandler<ShipmentCreatedDomainEvent>,
    IDomainEventHandler<DriverAssignedDomainEvent>,
    IDomainEventHandler<ShipmentPickedUpDomainEvent>,
    IDomainEventHandler<ShipmentInTransitDomainEvent>,
    IDomainEventHandler<ShipmentDeliveredDomainEvent>,
    IDomainEventHandler<ShipmentCancelledDomainEvent>,
    IDomainEventHandler<ShipmentDeliveryPaymentFailedDomainEvent>
{
    public Task HandleAsync(ShipmentCreatedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentCreatedIntegrationEvent(
            e.ShipmentId.Value,
            e.CustomerId.Value,
            ToDto(e.PickupAddress),
            ToDto(e.DeliveryAddress),
            e.TotalWeight.Kilograms,
            e.ItemCount,
            e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(DriverAssignedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentAssignedIntegrationEvent(e.ShipmentId.Value, e.CustomerId.Value, e.DriverId.Value, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ShipmentPickedUpDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentPickedUpIntegrationEvent(e.ShipmentId.Value, e.CustomerId.Value, e.DriverId.Value, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ShipmentInTransitDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentInTransitIntegrationEvent(e.ShipmentId.Value, e.CustomerId.Value, e.DriverId.Value, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ShipmentDeliveredDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentDeliveredIntegrationEvent(e.ShipmentId.Value, e.CustomerId.Value, e.DriverId.Value, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ShipmentCancelledDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentCancelledIntegrationEvent(e.ShipmentId.Value, e.CustomerId.Value, e.DriverId?.Value, e.Reason, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ShipmentDeliveryPaymentFailedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new ShipmentDeliveryPaymentFailedIntegrationEvent(e.ShipmentId.Value, e.CustomerId.Value, e.Reason, e.OccurredAt));
        return Task.CompletedTask;
    }

    private static AddressDto ToDto(Address address) => new(address.Street, address.City, address.PostalCode);
}
