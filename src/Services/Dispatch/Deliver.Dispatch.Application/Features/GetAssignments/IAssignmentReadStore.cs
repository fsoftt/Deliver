namespace Deliver.Dispatch.Application.Features.GetAssignments;

public interface IAssignmentReadStore
{
    Task<AssignmentView?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssignmentView>> ListAsync(string? status, CancellationToken cancellationToken);
}

public sealed record AssignmentView(
    Guid Id,
    Guid ShipmentId,
    string Status,
    Guid? DriverId,
    string? DriverName,
    decimal RequiredCapacityKg,
    IReadOnlyCollection<Guid> RejectedDriverIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AssignedAt);
