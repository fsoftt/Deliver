using Deliver.Dispatch.Domain.Assignments;
using Deliver.Dispatch.Domain.Drivers;

namespace Deliver.Dispatch.Domain;

public interface IAssignmentRepository
{
    Task<DeliveryAssignment?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    /// <summary>Oldest pending assignments a vehicle of <paramref name="maxCapacityKg"/> could carry.</summary>
    Task<IReadOnlyList<DeliveryAssignment>> ListPendingAsync(decimal maxCapacityKg, int take, CancellationToken cancellationToken);

    void Add(DeliveryAssignment assignment);
}

public interface IDriverPool
{
    Task<Driver?> GetAsync(Guid driverId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Driver>> ListAvailableAsync(decimal minCapacityKg, CancellationToken cancellationToken);

    void Add(Driver driver);
}
