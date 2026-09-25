using Deliver.Shipping.Application.Features.ShipmentLifecycle;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Deliver.Shipping.Api.Features;

public static class ShipmentLifecycleEndpoints
{
    public sealed record CancelShipmentRequest(string? Reason);

    public static RouteGroupBuilder MapShipmentLifecycle(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/pickup", (Guid id, ISender sender, CancellationToken ct) =>
                SendAsync(sender, new PickUpShipmentCommand(id), ct))
            .WithName("PickUpShipment")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/transit", (Guid id, ISender sender, CancellationToken ct) =>
                SendAsync(sender, new StartTransitCommand(id), ct))
            .WithName("StartTransit")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deliver", (Guid id, ISender sender, CancellationToken ct) =>
                SendAsync(sender, new DeliverShipmentCommand(id), ct))
            .WithName("DeliverShipment")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/cancel", (Guid id, CancelShipmentRequest request, ISender sender, CancellationToken ct) =>
                SendAsync(sender, new CancelShipmentCommand(id, request.Reason), ct))
            .WithName("CancelShipment")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<NoContent> SendAsync(ISender sender, IRequest command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return TypedResults.NoContent();
    }
}
