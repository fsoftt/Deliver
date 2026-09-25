using Deliver.SharedKernel;

namespace Deliver.Fleet.Domain.Drivers;

/// <summary>
/// A driver and their working state. Fleet is the source of truth for availability:
/// <code>
/// Unavailable ⇄ Available → OnDelivery → Available
/// </code>
/// </summary>
public sealed class Driver : AggregateRoot<DriverId>
{
    private Driver()
    {
    }

    public string Name { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public Vehicle? Vehicle { get; private set; }
    public DriverAvailability Availability { get; private set; }

    /// <summary>Shipment being delivered while <see cref="DriverAvailability.OnDelivery"/>. Shipments are owned by Shipping.</summary>
    public Guid? CurrentShipmentId { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }
    public uint Version { get; private set; }

    /// <summary>New drivers start off shift; they go online when their shift starts.</summary>
    public static Driver Register(string? name, string? phone, Vehicle? vehicle, DateTimeOffset now) => new()
    {
        Id = DriverId.New(),
        Name = Ensure.NotEmpty(name, "Name", maxLength: 100),
        Phone = Ensure.NotEmpty(phone, "Phone", maxLength: 30),
        Vehicle = vehicle,
        Availability = DriverAvailability.Unavailable,
        RegisteredAt = now,
    };

    public void AssignVehicle(Vehicle vehicle)
    {
        if (Availability == DriverAvailability.OnDelivery)
            throw new DomainException("Cannot change vehicle during a delivery.");

        Vehicle = vehicle;
    }

    public void GoOnline(DateTimeOffset now)
    {
        switch (Availability)
        {
            case DriverAvailability.Available:
                return;
            case DriverAvailability.OnDelivery:
                throw new DomainException("A driver on a delivery is already working.");
        }

        if (Vehicle is null)
            throw new DomainException("A driver needs a vehicle before going online.");

        Availability = DriverAvailability.Available;
        Raise(new DriverBecameAvailableDomainEvent(Id, Name, Vehicle.CapacityKg, now));
    }

    public void GoOffline(DateTimeOffset now)
    {
        switch (Availability)
        {
            case DriverAvailability.Unavailable:
                return;
            case DriverAvailability.OnDelivery:
                throw new DomainException("A driver cannot go offline in the middle of a delivery.");
        }

        Availability = DriverAvailability.Unavailable;
        Raise(new DriverBecameUnavailableDomainEvent(Id, "OffShift", now));
    }

    /// <summary>
    /// Dispatch assigned this driver based on its (eventually consistent) copy of availability.
    /// Fleet has the final word: if the driver is no longer available, the assignment is rejected
    /// (an expected business outcome, not an error) so Dispatch can choose someone else.
    /// </summary>
    public void AcceptAssignment(Guid shipmentId, DateTimeOffset now)
    {
        if (Availability == DriverAvailability.OnDelivery && CurrentShipmentId == shipmentId)
            return; // Already accepted.

        if (Availability != DriverAvailability.Available)
        {
            Raise(new DriverAssignmentRejectedDomainEvent(Id, shipmentId, $"Driver is {Availability}.", now));
            return;
        }

        Availability = DriverAvailability.OnDelivery;
        CurrentShipmentId = shipmentId;
        Raise(new DriverBecameUnavailableDomainEvent(Id, "OnDelivery", now));
    }

    /// <summary>The shipment was delivered or cancelled: the driver is free again.</summary>
    public void ReleaseFromShipment(Guid shipmentId, DateTimeOffset now)
    {
        if (Availability != DriverAvailability.OnDelivery || CurrentShipmentId != shipmentId)
            return; // Not (or no longer) working on this shipment.

        Availability = DriverAvailability.Available;
        CurrentShipmentId = null;
        Raise(new DriverBecameAvailableDomainEvent(Id, Name, Vehicle!.CapacityKg, now));
    }
}
