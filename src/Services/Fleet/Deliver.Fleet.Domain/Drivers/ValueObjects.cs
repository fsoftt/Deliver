using Deliver.SharedKernel;

namespace Deliver.Fleet.Domain.Drivers;

public readonly record struct DriverId(Guid Value)
{
    public static DriverId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public enum VehicleType
{
    Motorcycle,
    Car,
    Van,
}

/// <summary>The vehicle a driver works with. Replaced as a whole, so it is a value object.</summary>
public sealed record Vehicle
{
    private Vehicle()
    {
    }

    public string Plate { get; private init; } = null!;
    public VehicleType Type { get; private init; }
    public decimal CapacityKg { get; private init; }

    public static Vehicle Create(string? plate, VehicleType type, decimal capacityKg) => new()
    {
        Plate = Ensure.NotEmpty(plate, "Plate", maxLength: 12).ToUpperInvariant(),
        Type = type,
        CapacityKg = Ensure.Positive(capacityKg, "Vehicle capacity"),
    };
}

public enum DriverAvailability
{
    Unavailable,
    Available,
    OnDelivery,
}
