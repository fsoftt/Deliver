using Deliver.Contracts.Dispatch;
using Deliver.Dispatch.Domain.Assignments;
using Deliver.Messaging;
using Deliver.SharedKernel;

namespace Deliver.Dispatch.Application.IntegrationEvents;

public sealed class PublishDispatchIntegrationEvents(IOutbox outbox) : IDomainEventHandler<DriverAssignedDomainEvent>
{
    public Task HandleAsync(DriverAssignedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new DriverAssignedIntegrationEvent(e.AssignmentId, e.ShipmentId, e.DriverId, e.DriverName, e.OccurredAt));
        return Task.CompletedTask;
    }
}
