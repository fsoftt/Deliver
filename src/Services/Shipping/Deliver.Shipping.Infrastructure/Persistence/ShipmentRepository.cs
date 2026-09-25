using Deliver.Shipping.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Shipping.Infrastructure.Persistence;

internal sealed class ShipmentRepository(ShippingDbContext db) : IShipmentRepository
{
    public Task<Shipment?> GetAsync(ShipmentId id, CancellationToken cancellationToken) =>
        db.Shipments.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Shipment shipment) => db.Shipments.Add(shipment);
}
