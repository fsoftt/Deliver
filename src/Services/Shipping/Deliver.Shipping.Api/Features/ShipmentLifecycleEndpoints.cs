using Deliver.Shipping.Application.Features.ShipmentLifecycle;

namespace Deliver.Shipping.Api.Features;

public static class ShipmentLifecycleEndpoints
{
    public sealed record CancelShipmentRequest(string? Reason);

    public static RouteGroupBuilder MapShipmentLifecycle(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/pickup", async (Guid id, PickUpShipmentHandler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(id, ct);
                return TypedResults.NoContent();
            })
            .WithName("PickUpShipment")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/transit", async (Guid id, StartTransitHandler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(id, ct);
                return TypedResults.NoContent();
            })
            .WithName("StartTransit")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deliver", async (Guid id, DeliverShipmentHandler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(id, ct);
                return TypedResults.NoContent();
            })
            .WithName("DeliverShipment")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelShipmentRequest request, CancelShipmentHandler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(new CancelShipmentCommand(id, request.Reason), ct);
                return TypedResults.NoContent();
            })
            .WithName("CancelShipment")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
