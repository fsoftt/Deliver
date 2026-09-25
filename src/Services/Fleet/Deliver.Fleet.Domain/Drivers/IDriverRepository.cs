namespace Deliver.Fleet.Domain.Drivers;

public interface IDriverRepository
{
    Task<Driver?> GetAsync(DriverId id, CancellationToken cancellationToken);

    void Add(Driver driver);
}
