using Deliver.Contracts.Dispatch;
using Deliver.Contracts.Shipping;
using Deliver.Fleet.Application.Features.Deliveries;
using Deliver.Fleet.Application.Features.GetDrivers;
using Deliver.Fleet.Domain.Drivers;
using Deliver.Fleet.Infrastructure.Persistence;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Fleet.Infrastructure;

public static class DependencyInjection
{
    public const string QueueName = "fleet";

    public static IServiceCollection AddFleetInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily so hosts and tests can override configuration after registration.
        services.AddDbContext<FleetDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("FleetDb")
                ?? throw new InvalidOperationException("Connection string 'FleetDb' is missing."))
            .UseSnakeCaseNamingConvention()
            .UseDomainEvents(sp));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FleetDbContext>());
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IDriverReadStore, DriverReadStore>();

        services.AddHealthChecks().AddDbContextCheck<FleetDbContext>("database", tags: ["ready"]);

        services.AddMessaging(configuration, clientName: "fleet-service")
            .AddOutbox<FleetDbContext>()
            .AddConsumer<FleetDbContext>(QueueName, consumer => consumer
                .Subscribe<DriverAssignedIntegrationEvent, DriverAssignedHandler>()
                .Subscribe<ShipmentDeliveredIntegrationEvent, ShipmentDeliveredHandler>()
                .Subscribe<ShipmentCancelledIntegrationEvent, ShipmentCancelledHandler>());

        return services;
    }

    public static async Task MigrateFleetDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FleetDbContext>().Database.MigrateAsync();
    }
}
