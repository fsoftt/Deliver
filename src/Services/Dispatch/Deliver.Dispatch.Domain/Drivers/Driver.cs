using Deliver.SharedKernel;

namespace Deliver.Dispatch.Domain.Drivers;

/// <summary>
/// Dispatch's OWN model of a driver: only what matching needs (capacity, availability, fairness).
/// It is built from Fleet's events (a local projection), so Dispatch never queries Fleet at runtime.
/// Same word as Fleet's Driver, different bounded context, different model.
/// </summary>
public sealed class Driver : AggregateRoot<Guid>
{
    private Driver()
    {
    }

    public string Name { get; private set; } = null!;
    public decimal CapacityKg { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset? LastAssignedAt { get; private set; }

    /// <summary>Time of the latest Fleet fact applied; older (re-ordered or retried) facts are ignored.</summary>
    public DateTimeOffset LastChangedAt { get; private set; }

    public uint Version { get; private set; }

    public static Driver FromFleet(Guid driverId, string name, decimal capacityKg, DateTimeOffset since) => new()
    {
        Id = driverId,
        Name = name,
        CapacityKg = capacityKg,
        IsAvailable = true,
        LastChangedAt = since,
    };

    public void MarkAvailable(string name, decimal capacityKg, DateTimeOffset since)
    {
        if (since < LastChangedAt)
            return;

        Name = name;
        CapacityKg = capacityKg;
        IsAvailable = true;
        LastChangedAt = since;
    }

    public void MarkUnavailable(DateTimeOffset since)
    {
        if (since < LastChangedAt)
            return;

        IsAvailable = false;
        LastChangedAt = since;
    }

    /// <summary>
    /// Takes the driver out of the pool immediately so the next shipment does not get the same driver
    /// before Fleet's confirmation (driver.unavailable) arrives.
    /// </summary>
    internal void Reserve(DateTimeOffset now)
    {
        if (!IsAvailable)
            throw new DomainException("Driver cannot be assigned when unavailable.");

        IsAvailable = false;
        LastAssignedAt = now;
        LastChangedAt = now;
    }
}
