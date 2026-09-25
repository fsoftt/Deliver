using Deliver.Contracts.Dispatch;
using Deliver.Contracts.Shipping;
using Deliver.Fleet.Domain.Drivers;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Deliver.Fleet.Application.Features.Deliveries;

/// <summary>Dispatch picked this driver. Fleet accepts, or rejects if the driver is no longer available.</summary>
public sealed class DriverAssignedHandler(IDriverRepository drivers, IUnitOfWork unitOfWork, TimeProvider clock)
    : IIntegrationEventHandler<DriverAssignedIntegrationEvent>
{
    public async Task HandleAsync(DriverAssignedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetAsync(new DriverId(integrationEvent.DriverId), cancellationToken)
            ?? throw new NotFoundException("Driver", integrationEvent.DriverId);

        driver.AcceptAssignment(integrationEvent.ShipmentId, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ShipmentDeliveredHandler(IDriverRepository drivers, IUnitOfWork unitOfWork, TimeProvider clock)
    : IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>
{
    public async Task HandleAsync(ShipmentDeliveredIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetAsync(new DriverId(integrationEvent.DriverId), cancellationToken)
            ?? throw new NotFoundException("Driver", integrationEvent.DriverId);

        driver.ReleaseFromShipment(integrationEvent.ShipmentId, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Compensation: a cancelled shipment frees its driver.</summary>
public sealed class ShipmentCancelledHandler(
    IDriverRepository drivers,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ShipmentCancelledHandler> logger) : IIntegrationEventHandler<ShipmentCancelledIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCancelledIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        if (integrationEvent.DriverId is not { } driverId)
            return;

        var driver = await drivers.GetAsync(new DriverId(driverId), cancellationToken);
        if (driver is null)
        {
            logger.LogWarning("Cancelled shipment {ShipmentId} references unknown driver {DriverId}", integrationEvent.ShipmentId, driverId);
            return;
        }

        driver.ReleaseFromShipment(integrationEvent.ShipmentId, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
