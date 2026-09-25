using Deliver.Dispatch.Application;
using Deliver.Dispatch.Application.Features.GetAssignments;
using Deliver.Dispatch.Infrastructure;
using Deliver.ServiceDefaults;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("dispatch-service");
builder.Services.AddOpenApi();
builder.Services.AddDispatchApplication();
builder.Services.AddDispatchInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapOpenApi();
app.MapDefaultEndpoints();

// Dispatch is driven by events; its HTTP surface is read-only, for operators.
var assignments = app.MapGroup("/api/dispatch/assignments").WithTags("Assignments");

assignments.MapGet("/", async (IAssignmentReadStore store, string? status, CancellationToken ct) =>
        TypedResults.Ok(await store.ListAsync(status, ct)))
    .WithName("ListAssignments");

assignments.MapGet("/{shipmentId:guid}", async Task<Results<Ok<AssignmentView>, NotFound>> (
        Guid shipmentId, IAssignmentReadStore store, CancellationToken ct) =>
        await store.GetByShipmentAsync(shipmentId, ct) is { } view ? TypedResults.Ok(view) : TypedResults.NotFound())
    .WithName("GetAssignmentByShipment");

await app.Services.MigrateDispatchDatabaseAsync();
await app.RunAsync();

public partial class Program;
