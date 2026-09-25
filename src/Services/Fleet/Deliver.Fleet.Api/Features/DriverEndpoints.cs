using Deliver.Fleet.Application.Features.GetDrivers;
using Deliver.Fleet.Application.Features.ManageDriver;
using Deliver.Fleet.Application.Features.RegisterDriver;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Deliver.Fleet.Api.Features;

public static class DriverEndpoints
{
    public sealed record ChangeAvailabilityRequest(bool Available);

    public static RouteGroupBuilder MapDriverEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (RegisterDriverCommand command, RegisterDriverHandler handler, CancellationToken ct) =>
            {
                var id = await handler.HandleAsync(command, ct);
                return TypedResults.Created($"/api/drivers/{id}", new { id = id.Value });
            })
            .WithName("RegisterDriver");

        group.MapGet("/{id:guid}", async Task<Results<Ok<DriverDetails>, NotFound>> (Guid id, IDriverReadStore store, CancellationToken ct) =>
                await store.GetAsync(id, ct) is { } driver ? TypedResults.Ok(driver) : TypedResults.NotFound())
            .WithName("GetDriver");

        group.MapGet("/available", async (IDriverReadStore store, CancellationToken ct) =>
                TypedResults.Ok(await store.ListAvailableAsync(ct)))
            .WithName("ListAvailableDrivers");

        group.MapPost("/{id:guid}/availability", async (Guid id, ChangeAvailabilityRequest request, ChangeAvailabilityHandler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(new ChangeAvailabilityCommand(id, request.Available), ct);
                return TypedResults.NoContent();
            })
            .WithName("ChangeDriverAvailability")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/vehicle", async (Guid id, VehicleInput vehicle, AssignVehicleHandler handler, CancellationToken ct) =>
            {
                await handler.HandleAsync(new AssignVehicleCommand(id, vehicle), ct);
                return TypedResults.NoContent();
            })
            .WithName("AssignVehicle")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
