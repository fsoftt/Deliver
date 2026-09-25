using Deliver.Analytics.Data;
using Deliver.Analytics.Features.Projection;
using Deliver.Analytics.Features.Reports;
using Deliver.Contracts.Billing;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("analytics-service");
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AnalyticsDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("AnalyticsDb")
        ?? throw new InvalidOperationException("Connection string 'AnalyticsDb' is missing."))
    .UseSnakeCaseNamingConvention());
builder.Services.AddHealthChecks().AddDbContextCheck<AnalyticsDbContext>("database", tags: ["ready"]);

// Never queries another service's database: everything it knows arrives as events.
builder.Services.AddMessaging(builder.Configuration, clientName: "analytics-service")
    .AddConsumer<AnalyticsDbContext>("analytics", consumer => consumer
        .Subscribe<ShipmentCreatedIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<ShipmentAssignedIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<ShipmentPickedUpIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<ShipmentInTransitIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<ShipmentDeliveredIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<ShipmentCancelledIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<PaymentCapturedIntegrationEvent, ShipmentFactProjection>()
        .Subscribe<ShipmentDeliveryPaymentFailedIntegrationEvent, ShipmentFactProjection>());

var app = builder.Build();

app.UseServiceDefaults();
app.MapOpenApi();
app.MapDefaultEndpoints();
app.MapReports();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync();

await app.RunAsync();

public partial class Program;
