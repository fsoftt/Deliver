using Deliver.Contracts.Billing;
using Deliver.Contracts.Dispatch;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Deliver.Shipping.Application.Features.AssignDriver;
using Deliver.Shipping.Application.Features.Billing;
using Deliver.Shipping.Application.Features.GetShipment;
using Deliver.Shipping.Domain.Shipments;
using Deliver.Shipping.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Shipping.Infrastructure;

public static class DependencyInjection
{
    public const string QueueName = "shipping";

    public static IServiceCollection AddShippingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily so hosts and tests can override configuration after registration.
        services.AddDbContext<ShippingDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("ShippingDb")
                ?? throw new InvalidOperationException("Connection string 'ShippingDb' is missing."))
            .UseSnakeCaseNamingConvention()
            .UseDomainEvents(sp));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ShippingDbContext>());
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IShipmentReadStore, ShipmentReadStore>();

        services.AddHealthChecks().AddDbContextCheck<ShippingDbContext>("database", tags: ["ready"]);

        services.AddMessaging(configuration, clientName: "shipping-service")
            .AddOutbox<ShippingDbContext>()
            .AddConsumer<ShippingDbContext>(QueueName, consumer => consumer
                .Subscribe<DriverAssignedIntegrationEvent, DriverAssignedHandler>()
                .Subscribe<DeliveryPriceCalculatedIntegrationEvent, DeliveryPriceCalculatedHandler>()
                .Subscribe<PaymentCapturedIntegrationEvent, PaymentCapturedHandler>()
                .Subscribe<PaymentFailedIntegrationEvent, PaymentFailedHandler>());

        return services;
    }

    /// <summary>Applies pending EF Core migrations. Convenient for the demo; see ADR-0006 for production notes.</summary>
    public static async Task MigrateShippingDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ShippingDbContext>().Database.MigrateAsync();
    }
}
