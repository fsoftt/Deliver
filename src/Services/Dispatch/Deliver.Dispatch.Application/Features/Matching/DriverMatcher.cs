using Deliver.Dispatch.Domain;
using Deliver.Dispatch.Domain.Assignments;
using Deliver.Dispatch.Domain.Drivers;
using Microsoft.Extensions.Logging;

namespace Deliver.Dispatch.Application.Features.Matching;

/// <summary>
/// Application service shared by the handlers that can create a match: new work arriving
/// (shipment created / driver rejected) or a driver becoming free (driver available).
/// </summary>
public sealed class DriverMatcher(
    IAssignmentRepository assignments,
    IDriverPool drivers,
    TimeProvider clock,
    ILogger<DriverMatcher> logger)
{
    /// <summary>Finds a driver for one pending assignment, if anyone suitable is available.</summary>
    public async Task<bool> TryAssignAsync(DeliveryAssignment assignment, CancellationToken cancellationToken)
    {
        var candidates = await drivers.ListAvailableAsync(assignment.RequiredCapacityKg, cancellationToken);
        var driver = DriverSelectionPolicy.SelectFor(assignment, candidates);

        if (driver is null)
        {
            logger.LogInformation("No suitable driver for shipment {ShipmentId} ({Kg} kg); assignment stays pending",
                assignment.ShipmentId, assignment.RequiredCapacityKg);
            return false;
        }

        assignment.AssignTo(driver, clock.GetUtcNow());
        logger.LogInformation("Assigned driver {DriverId} to shipment {ShipmentId}", driver.Id, assignment.ShipmentId);
        return true;
    }

    /// <summary>Gives a newly available driver the oldest pending shipment they can carry.</summary>
    public async Task<bool> TryGiveWorkToAsync(Driver driver, CancellationToken cancellationToken)
    {
        if (!driver.IsAvailable)
            return false;

        var pending = await assignments.ListPendingAsync(driver.CapacityKg, take: 20, cancellationToken);
        var assignment = pending.FirstOrDefault(a => a.CanBeCarriedBy(driver));
        if (assignment is null)
            return false;

        assignment.AssignTo(driver, clock.GetUtcNow());
        logger.LogInformation("Assigned newly available driver {DriverId} to pending shipment {ShipmentId}",
            driver.Id, assignment.ShipmentId);
        return true;
    }
}
