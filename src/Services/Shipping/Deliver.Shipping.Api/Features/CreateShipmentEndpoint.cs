using Deliver.Shipping.Application.Features.CreateShipment;

namespace Deliver.Shipping.Api.Features;

public static class CreateShipmentEndpoint
{
    public static RouteGroupBuilder MapCreateShipment(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateShipmentCommand command, CreateShipmentHandler handler, CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(command, ct);
                return TypedResults.Created($"/api/shipments/{id}", new { id = id.Value });
            })
            .WithName("CreateShipment")
            .WithSummary("Create a shipment. Price and driver are assigned asynchronously.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }
}
