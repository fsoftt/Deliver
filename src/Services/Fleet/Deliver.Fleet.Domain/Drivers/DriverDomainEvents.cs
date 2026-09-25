using Deliver.SharedKernel;

namespace Deliver.Fleet.Domain.Drivers;

public sealed record DriverBecameAvailableDomainEvent(
    DriverId DriverId,
    string Name,
    decimal VehicleCapacityKg,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record DriverBecameUnavailableDomainEvent(
    DriverId DriverId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record DriverAssignmentRejectedDomainEvent(
    DriverId DriverId,
    Guid ShipmentId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
