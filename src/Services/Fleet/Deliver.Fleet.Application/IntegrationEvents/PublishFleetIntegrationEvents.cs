using Deliver.Contracts.Fleet;
using Deliver.Fleet.Domain.Drivers;
using Deliver.Messaging;
using Deliver.SharedKernel;

namespace Deliver.Fleet.Application.IntegrationEvents;

/// <summary>Translates Fleet domain events into public contracts (stored in the outbox).</summary>
public sealed class PublishFleetIntegrationEvents(IOutbox outbox) :
    IDomainEventHandler<DriverBecameAvailableDomainEvent>,
    IDomainEventHandler<DriverBecameUnavailableDomainEvent>,
    IDomainEventHandler<DriverAssignmentRejectedDomainEvent>
{
    public Task HandleAsync(DriverBecameAvailableDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new DriverAvailableIntegrationEvent(e.DriverId.Value, e.Name, e.VehicleCapacityKg, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(DriverBecameUnavailableDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new DriverUnavailableIntegrationEvent(e.DriverId.Value, e.Reason, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(DriverAssignmentRejectedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new DriverAssignmentRejectedIntegrationEvent(e.ShipmentId, e.DriverId.Value, e.Reason, e.OccurredAt));
        return Task.CompletedTask;
    }
}
