using Deliver.Contracts.Fleet;
using Deliver.Dispatch.Application.Features.Matching;
using Deliver.Dispatch.Domain;
using Deliver.Dispatch.Domain.Drivers;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Deliver.Dispatch.Application.Features.Drivers;

/// <summary>Keeps the local driver pool in sync with Fleet and gives the driver pending work.</summary>
public sealed class DriverAvailableHandler(IDriverPool drivers, DriverMatcher matcher, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<DriverAvailableIntegrationEvent>
{
    public async Task HandleAsync(DriverAvailableIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetAsync(integrationEvent.DriverId, cancellationToken);
        if (driver is null)
        {
            driver = Driver.FromFleet(integrationEvent.DriverId, integrationEvent.Name, integrationEvent.VehicleCapacityKg, integrationEvent.AvailableSince);
            drivers.Add(driver);
        }
        else
        {
            driver.MarkAvailable(integrationEvent.Name, integrationEvent.VehicleCapacityKg, integrationEvent.AvailableSince);
        }

        await matcher.TryGiveWorkToAsync(driver, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DriverUnavailableHandler(IDriverPool drivers, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<DriverUnavailableIntegrationEvent>
{
    public async Task HandleAsync(DriverUnavailableIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetAsync(integrationEvent.DriverId, cancellationToken);
        if (driver is null)
            return; // Never seen as available, so never a candidate: nothing to do.

        driver.MarkUnavailable(integrationEvent.UnavailableSince);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>Fleet refused our choice: re-open the assignment and try the next best driver.</summary>
public sealed class DriverAssignmentRejectedHandler(
    IAssignmentRepository assignments,
    IDriverPool drivers,
    DriverMatcher matcher,
    IUnitOfWork unitOfWork,
    ILogger<DriverAssignmentRejectedHandler> logger) : IIntegrationEventHandler<DriverAssignmentRejectedIntegrationEvent>
{
    public async Task HandleAsync(DriverAssignmentRejectedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var assignment = await assignments.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment for shipment", integrationEvent.ShipmentId);

        logger.LogWarning("Driver {DriverId} rejected shipment {ShipmentId}: {Reason}",
            integrationEvent.DriverId, integrationEvent.ShipmentId, integrationEvent.Reason);

        assignment.RejectDriver(integrationEvent.DriverId);
        if (await drivers.GetAsync(integrationEvent.DriverId, cancellationToken) is { } driver)
            driver.MarkUnavailable(integrationEvent.RejectedAt);

        await matcher.TryAssignAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
