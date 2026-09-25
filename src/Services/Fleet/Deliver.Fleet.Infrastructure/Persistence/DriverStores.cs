using Deliver.Fleet.Application.Features.GetDrivers;
using Deliver.Fleet.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Fleet.Infrastructure.Persistence;

internal sealed class DriverRepository(FleetDbContext db) : IDriverRepository
{
    public Task<Driver?> GetAsync(DriverId id, CancellationToken cancellationToken) =>
        db.Drivers.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(Driver driver) => db.Drivers.Add(driver);
}

internal sealed class DriverReadStore(FleetDbContext db) : IDriverReadStore
{
    public async Task<DriverDetails?> GetAsync(Guid driverId, CancellationToken cancellationToken)
    {
        var id = new DriverId(driverId);
        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return driver is null ? null : ToDetails(driver);
    }

    public async Task<IReadOnlyList<DriverDetails>> ListAsync(bool onlyAvailable, CancellationToken cancellationToken)
    {
        var query = db.Drivers.AsNoTracking();
        if (onlyAvailable)
            query = query.Where(d => d.Availability == DriverAvailability.Available);

        var drivers = await query.OrderBy(d => d.Name).Take(200).ToListAsync(cancellationToken);
        return drivers.Select(ToDetails).ToList();
    }

    private static DriverDetails ToDetails(Driver d) => new(
        d.Id.Value,
        d.Name,
        d.Phone,
        d.Availability.ToString(),
        d.Vehicle is null ? null : new VehicleView(d.Vehicle.Plate, d.Vehicle.Type.ToString(), d.Vehicle.CapacityKg),
        d.CurrentShipmentId,
        d.RegisteredAt);
}
