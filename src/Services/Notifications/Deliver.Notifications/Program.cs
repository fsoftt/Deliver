using Deliver.Contracts.Dispatch;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.Notifications.Data;
using Deliver.Notifications.Features.History;
using Deliver.Notifications.Features.Notify;
using Deliver.Notifications.Providers;
using Deliver.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("notification-service");
builder.Services.AddOpenApi();

builder.Services.AddDbContext<NotificationsDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("NotificationsDb")
        ?? throw new InvalidOperationException("Connection string 'NotificationsDb' is missing."))
    .UseSnakeCaseNamingConvention());
builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>("database", tags: ["ready"]);

builder.Services.Configure<FakeNotificationProviderOptions>(builder.Configuration.GetSection(FakeNotificationProviderOptions.SectionName));
builder.Services.AddSingleton<INotificationProvider, FakeNotificationProvider>();
builder.Services.AddScoped<CustomerNotifier>();

// Consumer only: Notifications publishes nothing, so it needs an inbox but no outbox.
builder.Services.AddMessaging(builder.Configuration, clientName: "notification-service")
    .AddConsumer<NotificationsDbContext>("notifications", consumer => consumer
        .Subscribe<ShipmentCreatedIntegrationEvent, ShipmentCreatedHandler>()
        .Subscribe<DriverAssignedIntegrationEvent, DriverAssignedHandler>()
        .Subscribe<ShipmentPickedUpIntegrationEvent, ShipmentPickedUpHandler>()
        .Subscribe<ShipmentInTransitIntegrationEvent, ShipmentInTransitHandler>()
        .Subscribe<ShipmentDeliveredIntegrationEvent, ShipmentDeliveredHandler>()
        .Subscribe<ShipmentCancelledIntegrationEvent, ShipmentCancelledHandler>()
        .Subscribe<ShipmentDeliveryPaymentFailedIntegrationEvent, ShipmentDeliveryPaymentFailedHandler>());

var app = builder.Build();

app.UseServiceDefaults();
app.MapOpenApi();
app.MapDefaultEndpoints();
app.MapNotificationHistory();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();

await app.RunAsync();

public partial class Program;
