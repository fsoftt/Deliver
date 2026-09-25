using Deliver.SharedKernel;
using Deliver.Shipping.Application.Features.GetShipment;
using Deliver.Shipping.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Shipping.Infrastructure.Persistence;

internal sealed class ShipmentReadStore(ShippingDbContext db) : IShipmentReadStore
{
    public async Task<ShipmentDetails?> GetAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var id = new ShipmentId(shipmentId);
        var shipment = await db.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return shipment is null ? null : ToDetails(shipment);
    }

    public async Task<PagedResult<ShipmentSummary>> ListAsync(
        string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Shipments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ShipmentStatus>(status, ignoreCase: true, out var parsed))
                throw new InvalidValueException($"Unknown status '{status}'.");
            query = query.Where(s => s.Status == parsed);
        }

        var total = await query.CountAsync(cancellationToken);
        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = shipments
            .Select(s => new ShipmentSummary(s.Id.Value, s.CustomerId.Value, s.Status.ToString(), s.DriverId?.Value, s.Price?.Amount, s.CreatedAt))
            .ToList();

        return new PagedResult<ShipmentSummary>(items, page, pageSize, total);
    }

    private static ShipmentDetails ToDetails(Shipment s) => new(
        s.Id.Value,
        s.CustomerId.Value,
        s.Status.ToString(),
        s.DriverId?.Value,
        ToView(s.PickupAddress),
        ToView(s.DeliveryAddress),
        s.Items.Select(i => new ItemView(i.Description, i.Quantity, i.UnitWeight.Kilograms)).ToList(),
        s.TotalWeight.Kilograms,
        s.Price?.Amount,
        s.Price?.Currency,
        s.PaymentStatus.ToString(),
        s.CreatedAt,
        s.AssignedAt,
        s.PickedUpAt,
        s.InTransitAt,
        s.DeliveredAt,
        s.CancelledAt,
        s.CancellationReason);

    private static AddressView ToView(Address a) => new(a.Street, a.City, a.PostalCode);
}
