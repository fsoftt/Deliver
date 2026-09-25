using Deliver.Dispatch.Domain.Drivers;
using Deliver.SharedKernel;

namespace Deliver.Dispatch.Domain.Assignments;

public enum AssignmentStatus
{
    Pending,
    Assigned,
    Completed,
    Cancelled,
}

public sealed record DriverAssignedDomainEvent(
    Guid AssignmentId,
    Guid ShipmentId,
    Guid DriverId,
    string DriverName,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>
/// The work of getting ONE shipment a driver. Stays Pending while nobody suitable is available and is
/// retried whenever a driver becomes available or rejects.
/// </summary>
public sealed class DeliveryAssignment : AggregateRoot<Guid>
{
    private Guid[] _rejectedDrivers = [];

    private DeliveryAssignment()
    {
    }

    public Guid ShipmentId { get; private set; }
    public decimal RequiredCapacityKg { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public Guid? DriverId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }

    /// <summary>Drivers that Fleet rejected for this shipment; never offered again.</summary>
    public IReadOnlyCollection<Guid> RejectedDriverIds => _rejectedDrivers;

    public uint Version { get; private set; }

    public static DeliveryAssignment Open(Guid shipmentId, decimal requiredCapacityKg, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        ShipmentId = Ensure.NotEmpty(shipmentId, "Shipment id"),
        RequiredCapacityKg = Ensure.Positive(requiredCapacityKg, "Required capacity"),
        Status = AssignmentStatus.Pending,
        CreatedAt = now,
    };

    public bool CanBeCarriedBy(Driver driver) =>
        driver.IsAvailable
        && driver.CapacityKg >= RequiredCapacityKg
        && !_rejectedDrivers.Contains(driver.Id);

    public void AssignTo(Driver driver, DateTimeOffset now)
    {
        if (Status != AssignmentStatus.Pending)
            throw new DomainException($"Cannot assign a driver to an assignment that is {Status}.");

        if (!driver.IsAvailable)
            throw new DomainException("Driver cannot be assigned when unavailable.");

        if (driver.CapacityKg < RequiredCapacityKg)
            throw new DomainException($"Driver's vehicle ({driver.CapacityKg} kg) cannot carry {RequiredCapacityKg} kg.");

        if (_rejectedDrivers.Contains(driver.Id))
            throw new DomainException("Driver already rejected this shipment.");

        driver.Reserve(now);
        DriverId = driver.Id;
        Status = AssignmentStatus.Assigned;
        AssignedAt = now;
        Raise(new DriverAssignedDomainEvent(Id, ShipmentId, driver.Id, driver.Name, now));
    }

    /// <summary>Fleet refused the driver: go back to Pending and remember not to ask them again.</summary>
    public void RejectDriver(Guid driverId)
    {
        if (Status != AssignmentStatus.Assigned || DriverId != driverId)
            return;

        _rejectedDrivers = [.. _rejectedDrivers, driverId];
        DriverId = null;
        AssignedAt = null;
        Status = AssignmentStatus.Pending;
    }

    public void Complete()
    {
        if (Status == AssignmentStatus.Completed)
            return;
        if (Status != AssignmentStatus.Assigned)
            throw new DomainException($"Cannot complete an assignment that is {Status}.");

        Status = AssignmentStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is AssignmentStatus.Completed or AssignmentStatus.Cancelled)
            return;

        Status = AssignmentStatus.Cancelled;
    }
}
