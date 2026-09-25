using Deliver.Contracts.Dispatch;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;
using Microsoft.Extensions.Logging;

namespace Deliver.Shipping.Application.Features.AssignDriver;

/// <summary>Dispatch decided who carries the shipment; Shipping records it on the aggregate.</summary>
public sealed class DriverAssignedHandler(
    IShipmentRepository shipments,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<DriverAssignedHandler> logger) : IIntegrationEventHandler<DriverAssignedIntegrationEvent>
{
    public async Task HandleAsync(DriverAssignedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetAsync(new ShipmentId(integrationEvent.ShipmentId), cancellationToken)
            ?? throw new NotFoundException("Shipment", integrationEvent.ShipmentId);

        // The customer may have cancelled while Dispatch was assigning. Dispatch will learn about the
        // cancellation from shipment.cancelled, so the stale assignment is simply ignored here.
        if (shipment.Status == ShipmentStatus.Cancelled)
        {
            logger.LogInformation("Ignoring driver assignment for cancelled shipment {ShipmentId}", shipment.Id);
            return;
        }

        shipment.AssignDriver(DriverId.From(integrationEvent.DriverId), clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
