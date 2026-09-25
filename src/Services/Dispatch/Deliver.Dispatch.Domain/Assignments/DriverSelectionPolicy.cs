using Deliver.Dispatch.Domain.Drivers;

namespace Deliver.Dispatch.Domain.Assignments;

/// <summary>
/// Domain service: which of the candidate drivers should carry this shipment?
/// <list type="number">
/// <item>Must be able to carry it (available, enough capacity, has not rejected it).</item>
/// <item>Fairness first: the driver who has waited longest since their last assignment.</item>
/// <item>Then the smallest vehicle that fits, keeping big vans free for big parcels.</item>
/// </list>
/// Deliberately simple: route optimisation is out of scope.
/// </summary>
public static class DriverSelectionPolicy
{
    public static Driver? SelectFor(DeliveryAssignment assignment, IEnumerable<Driver> candidates) =>
        candidates
            .Where(assignment.CanBeCarriedBy)
            .OrderBy(driver => driver.LastAssignedAt ?? DateTimeOffset.MinValue)
            .ThenBy(driver => driver.CapacityKg)
            .ThenBy(driver => driver.Id)
            .FirstOrDefault();
}
