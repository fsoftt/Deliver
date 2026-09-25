namespace Deliver.Shipping.Application.Features.GetShipment;

/// <summary>
/// Query side (CQRS-lite): reads return DTOs straight from the database and never load aggregates,
/// because queries have no invariants to protect.
/// </summary>
public interface IShipmentReadStore
{
    Task<ShipmentDetails?> GetAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<PagedResult<ShipmentSummary>> ListAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record ShipmentDetails(
    Guid Id,
    Guid CustomerId,
    string Status,
    Guid? DriverId,
    AddressView PickupAddress,
    AddressView DeliveryAddress,
    IReadOnlyList<ItemView> Items,
    decimal TotalWeightKg,
    decimal? Price,
    string? Currency,
    string PaymentStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? PickedUpAt,
    DateTimeOffset? InTransitAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason);

public sealed record AddressView(string Street, string City, string PostalCode);

public sealed record ItemView(string Description, int Quantity, decimal UnitWeightKg);

public sealed record ShipmentSummary(Guid Id, Guid CustomerId, string Status, Guid? DriverId, decimal? Price, DateTimeOffset CreatedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
