namespace Deliver.Fleet.Application.Features.GetDrivers;

public interface IDriverReadStore
{
    Task<DriverDetails?> GetAsync(Guid driverId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DriverDetails>> ListAsync(bool onlyAvailable, CancellationToken cancellationToken);
}

public sealed record DriverDetails(
    Guid Id,
    string Name,
    string Phone,
    string Availability,
    VehicleView? Vehicle,
    Guid? CurrentShipmentId,
    DateTimeOffset RegisteredAt);

public sealed record VehicleView(string Plate, string Type, decimal CapacityKg);
