using Deliver.Shipping.Application.Features.CreateShipment;
using MediatR;

namespace Deliver.Shipping.Api.Features;

public static class CreateShipmentEndpoint
{
    public static RouteGroupBuilder MapCreateShipment(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateShipmentCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return TypedResults.Created($"/api/shipments/{id}", new { id = id.Value });
            })
            .WithName("CreateShipment")
            .WithSummary("Create a shipment. Price and driver are assigned asynchronously.")
            .ProducesValidationProblem();

        return group;
    }
}
