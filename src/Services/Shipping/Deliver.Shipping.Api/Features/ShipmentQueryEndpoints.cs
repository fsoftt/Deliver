using Deliver.Shipping.Application.Features.GetShipment;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Deliver.Shipping.Api.Features;

public static class ShipmentQueryEndpoints
{
    public static RouteGroupBuilder MapShipmentQueries(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async Task<Results<Ok<ShipmentDetails>, NotFound>> (
                Guid id, IShipmentReadStore store, CancellationToken ct) =>
                await store.GetAsync(id, ct) is { } shipment ? TypedResults.Ok(shipment) : TypedResults.NotFound())
            .WithName("GetShipment");

        group.MapGet("/", async (IShipmentReadStore store, CancellationToken ct, string? status, int page = 1, int pageSize = 20) =>
                TypedResults.Ok(await store.ListAsync(status, page, pageSize, ct)))
            .WithName("ListShipments");

        return group;
    }
}
