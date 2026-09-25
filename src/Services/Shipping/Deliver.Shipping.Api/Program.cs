using Deliver.ServiceDefaults;
using Deliver.Shipping.Api.Features;
using Deliver.Shipping.Application;
using Deliver.Shipping.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("shipping-service");
builder.Services.AddOpenApi();
builder.Services.AddShippingApplication();
builder.Services.AddShippingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapOpenApi();
app.MapDefaultEndpoints();

var shipments = app.MapGroup("/api/shipments").WithTags("Shipments");
shipments.MapCreateShipment();
shipments.MapShipmentQueries();
shipments.MapShipmentLifecycle();

await app.Services.MigrateShippingDatabaseAsync();
await app.RunAsync();

/// <summary>Entry point marker for integration tests (WebApplicationFactory).</summary>
public partial class Program;
