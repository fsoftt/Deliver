using Deliver.Contracts.Shipping;
using Deliver.Dispatch.Application.Features.Matching;
using Deliver.Dispatch.Domain;
using Deliver.Dispatch.Domain.Assignments;
using Deliver.Messaging;
using Deliver.SharedKernel;

namespace Deliver.Dispatch.Application.Features.Shipments;

/// <summary>New work: open an assignment and try to match it right away.</summary>
public sealed class ShipmentCreatedHandler(
    IAssignmentRepository assignments,
    DriverMatcher matcher,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IIntegrationEventHandler<ShipmentCreatedIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCreatedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        if (await assignments.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken) is not null)
            return; // One assignment per shipment, whatever the broker does.

        var assignment = DeliveryAssignment.Open(integrationEvent.ShipmentId, integrationEvent.TotalWeightKg, clock.GetUtcNow());
        assignments.Add(assignment);

        await matcher.TryAssignAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ShipmentDeliveredHandler(IAssignmentRepository assignments, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>
{
    public async Task HandleAsync(ShipmentDeliveredIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var assignment = await assignments.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment for shipment", integrationEvent.ShipmentId);

        assignment.Complete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ShipmentCancelledHandler(IAssignmentRepository assignments, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<ShipmentCancelledIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCancelledIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        // If ShipmentCreated has not been processed yet this throws, and the retry sorts out the ordering.
        var assignment = await assignments.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment for shipment", integrationEvent.ShipmentId);

        assignment.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
