using Deliver.SharedKernel;

namespace Deliver.Shipping.Domain.Shipments;

public sealed record ShipmentCreatedDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    Address PickupAddress,
    Address DeliveryAddress,
    Weight TotalWeight,
    int ItemCount,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record DriverAssignedDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    DriverId DriverId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ShipmentPickedUpDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    DriverId DriverId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ShipmentInTransitDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    DriverId DriverId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ShipmentDeliveredDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    DriverId DriverId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ShipmentCancelledDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    DriverId? DriverId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ShipmentDeliveryPaymentFailedDomainEvent(
    ShipmentId ShipmentId,
    CustomerId CustomerId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
