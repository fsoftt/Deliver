using Deliver.Dispatch.Application.Features.GetAssignments;
using Deliver.Dispatch.Domain;
using Deliver.Dispatch.Domain.Assignments;
using Deliver.Dispatch.Domain.Drivers;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Dispatch.Infrastructure.Persistence;

internal sealed class AssignmentRepository(DispatchDbContext db) : IAssignmentRepository
{
    public Task<DeliveryAssignment?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        db.Assignments.FirstOrDefaultAsync(a => a.ShipmentId == shipmentId, cancellationToken);

    public async Task<IReadOnlyList<DeliveryAssignment>> ListPendingAsync(decimal maxCapacityKg, int take, CancellationToken cancellationToken) =>
        await db.Assignments
            .Where(a => a.Status == AssignmentStatus.Pending && a.RequiredCapacityKg <= maxCapacityKg)
            .OrderBy(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(DeliveryAssignment assignment) => db.Assignments.Add(assignment);
}

internal sealed class DriverPool(DispatchDbContext db) : IDriverPool
{
    public Task<Driver?> GetAsync(Guid driverId, CancellationToken cancellationToken) =>
        db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, cancellationToken);

    public async Task<IReadOnlyList<Driver>> ListAvailableAsync(decimal minCapacityKg, CancellationToken cancellationToken) =>
        await db.Drivers
            .Where(d => d.IsAvailable && d.CapacityKg >= minCapacityKg)
            .ToListAsync(cancellationToken);

    public void Add(Driver driver) => db.Drivers.Add(driver);
}

internal sealed class AssignmentReadStore(DispatchDbContext db) : IAssignmentReadStore
{
    public async Task<AssignmentView?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var assignments = await db.Assignments.AsNoTracking()
            .Where(a => a.ShipmentId == shipmentId)
            .ToListAsync(cancellationToken);
        return (await ToViewsAsync(assignments, cancellationToken)).FirstOrDefault();
    }

    public async Task<IReadOnlyList<AssignmentView>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        var query = db.Assignments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<AssignmentStatus>(status, ignoreCase: true, out var parsed))
                throw new InvalidValueException($"Unknown status '{status}'.");
            query = query.Where(a => a.Status == parsed);
        }

        var assignments = await query.OrderByDescending(a => a.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return await ToViewsAsync(assignments, cancellationToken);
    }

    private async Task<IReadOnlyList<AssignmentView>> ToViewsAsync(
        List<DeliveryAssignment> assignments, CancellationToken cancellationToken)
    {
        var driverIds = assignments.Where(a => a.DriverId.HasValue).Select(a => a.DriverId!.Value).Distinct().ToList();
        var names = await db.Drivers.AsNoTracking()
            .Where(d => driverIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        return assignments.Select(a => new AssignmentView(
                a.Id,
                a.ShipmentId,
                a.Status.ToString(),
                a.DriverId,
                a.DriverId is { } id ? names.GetValueOrDefault(id) : null,
                a.RequiredCapacityKg,
                a.RejectedDriverIds,
                a.CreatedAt,
                a.AssignedAt))
            .ToList();
    }
}
