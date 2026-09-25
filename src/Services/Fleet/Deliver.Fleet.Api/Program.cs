using Deliver.Fleet.Api.Features;
using Deliver.Fleet.Application;
using Deliver.Fleet.Infrastructure;
using Deliver.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("fleet-service");
builder.Services.AddOpenApi();
builder.Services.AddFleetApplication();
builder.Services.AddFleetInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapOpenApi();
app.MapDefaultEndpoints();
app.MapGroup("/api/drivers").WithTags("Drivers").MapDriverEndpoints();

await app.Services.MigrateFleetDatabaseAsync();
await app.RunAsync();

public partial class Program;
