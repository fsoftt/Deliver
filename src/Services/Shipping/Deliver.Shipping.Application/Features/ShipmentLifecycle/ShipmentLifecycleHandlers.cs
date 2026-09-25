using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;

namespace Deliver.Shipping.Application.Features.ShipmentLifecycle;

// The driver-facing lifecycle steps. Each one is a thin use case: load the aggregate, ask it to change
// state (the aggregate enforces the rules and raises the event), commit.

public sealed class PickUpShipmentHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task HandleAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetRequiredAsync(shipmentId, cancellationToken);
        shipment.MarkAsPickedUp(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StartTransitHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task HandleAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetRequiredAsync(shipmentId, cancellationToken);
        shipment.MarkAsInTransit(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeliverShipmentHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task HandleAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetRequiredAsync(shipmentId, cancellationToken);
        shipment.MarkAsDelivered(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record CancelShipmentCommand(Guid ShipmentId, string? Reason);

public sealed class CancelShipmentHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task HandleAsync(CancelShipmentCommand command, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetRequiredAsync(command.ShipmentId, cancellationToken);
        shipment.Cancel(command.Reason, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

internal static class ShipmentRepositoryExtensions
{
    public static async Task<Shipment> GetRequiredAsync(
        this IShipmentRepository shipments, Guid id, CancellationToken cancellationToken) =>
        await shipments.GetAsync(new ShipmentId(id), cancellationToken)
        ?? throw new NotFoundException("Shipment", id);
}
