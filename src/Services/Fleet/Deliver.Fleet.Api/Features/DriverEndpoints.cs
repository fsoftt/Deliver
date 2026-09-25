using Deliver.Fleet.Application.Features.GetDrivers;
using Deliver.Fleet.Application.Features.ManageDriver;
using Deliver.Fleet.Application.Features.RegisterDriver;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Deliver.Fleet.Api.Features;

public static class DriverEndpoints
{
    public sealed record ChangeAvailabilityRequest(bool Available);

    public static RouteGroupBuilder MapDriverEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (RegisterDriverCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return TypedResults.Created($"/api/drivers/{id}", new { id = id.Value });
            })
            .WithName("RegisterDriver")
            .ProducesValidationProblem();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new ListDriversQuery(OnlyAvailable: false), ct)))
            .WithName("ListDrivers");

        group.MapGet("/available", async (ISender sender, CancellationToken ct) =>
                TypedResults.Ok(await sender.Send(new ListDriversQuery(OnlyAvailable: true), ct)))
            .WithName("ListAvailableDrivers");

        group.MapGet("/{id:guid}", async Task<Results<Ok<DriverDetails>, NotFound>> (Guid id, ISender sender, CancellationToken ct) =>
                await sender.Send(new GetDriverQuery(id), ct) is { } driver ? TypedResults.Ok(driver) : TypedResults.NotFound())
            .WithName("GetDriver");

        group.MapPost("/{id:guid}/availability", async (Guid id, ChangeAvailabilityRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ChangeAvailabilityCommand(id, request.Available), ct);
                return TypedResults.NoContent();
            })
            .WithName("ChangeDriverAvailability")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/vehicle", async (Guid id, VehicleInput vehicle, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AssignVehicleCommand(id, vehicle), ct);
                return TypedResults.NoContent();
            })
            .WithName("AssignVehicle")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }
}
