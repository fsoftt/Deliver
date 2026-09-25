using Deliver.Shipping.Application.Features.GetShipment;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Deliver.Shipping.Api.Features;

public static class ShipmentQueryEndpoints
{
    public static RouteGroupBuilder MapShipmentQueries(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async Task<Results<Ok<ShipmentDetails>, NotFound>> (Guid id, ISender sender, CancellationToken ct) =>
                await sender.Send(new GetShipmentQuery(id), ct) is { } shipment ? TypedResults.Ok(shipment) : TypedResults.NotFound())
            .WithName("GetShipment");

        group.MapGet("/", async (ISender sender, CancellationToken ct, string? status, int page = 1, int pageSize = 20) =>
                TypedResults.Ok(await sender.Send(new ListShipmentsQuery(status, page, pageSize), ct)))
            .WithName("ListShipments");

        return group;
    }
}
