using Deliver.Contracts.Fleet;
using Deliver.Contracts.Shipping;
using Deliver.Dispatch.Application.Features.Drivers;
using Deliver.Dispatch.Application.Features.GetAssignments;
using Deliver.Dispatch.Application.Features.Shipments;
using Deliver.Dispatch.Domain;
using Deliver.Dispatch.Infrastructure.Persistence;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Dispatch.Infrastructure;

public static class DependencyInjection
{
    public const string QueueName = "dispatch";

    public static IServiceCollection AddDispatchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily so hosts and tests can override configuration after registration.
        services.AddDbContext<DispatchDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("DispatchDb")
                ?? throw new InvalidOperationException("Connection string 'DispatchDb' is missing."))
            .UseSnakeCaseNamingConvention()
            .UseDomainEvents(sp));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DispatchDbContext>());
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<IDriverPool, DriverPool>();
        services.AddScoped<IAssignmentReadStore, AssignmentReadStore>();

        services.AddHealthChecks().AddDbContextCheck<DispatchDbContext>("database", tags: ["ready"]);

        services.AddMessaging(configuration, clientName: "dispatch-service")
            .AddOutbox<DispatchDbContext>()
            .AddConsumer<DispatchDbContext>(QueueName, consumer => consumer
                .Subscribe<ShipmentCreatedIntegrationEvent, ShipmentCreatedHandler>()
                .Subscribe<ShipmentDeliveredIntegrationEvent, ShipmentDeliveredHandler>()
                .Subscribe<ShipmentCancelledIntegrationEvent, ShipmentCancelledHandler>()
                .Subscribe<DriverAvailableIntegrationEvent, DriverAvailableHandler>()
                .Subscribe<DriverUnavailableIntegrationEvent, DriverUnavailableHandler>()
                .Subscribe<DriverAssignmentRejectedIntegrationEvent, DriverAssignmentRejectedHandler>());

        return services;
    }

    public static async Task MigrateDispatchDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DispatchDbContext>().Database.MigrateAsync();
    }
}
