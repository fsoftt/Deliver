using Deliver.Billing.Application.Features.GetInvoice;
using Deliver.Billing.Application.Features.Payments;
using Deliver.Billing.Application.Features.Pricing;
using Deliver.Billing.Domain.Invoices;
using Deliver.Billing.Infrastructure.Payments;
using Deliver.Billing.Infrastructure.Persistence;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Billing.Infrastructure;

public static class DependencyInjection
{
    public const string QueueName = "billing";

    public static IServiceCollection AddBillingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily so hosts and tests can override configuration after registration.
        services.AddDbContext<BillingDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("BillingDb")
                ?? throw new InvalidOperationException("Connection string 'BillingDb' is missing."))
            .UseSnakeCaseNamingConvention()
            .UseDomainEvents(sp));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BillingDbContext>());
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IInvoiceReadStore, InvoiceReadStore>();

        services.Configure<FakePaymentGatewayOptions>(configuration.GetSection(FakePaymentGatewayOptions.SectionName));
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();

        services.AddHealthChecks().AddDbContextCheck<BillingDbContext>("database", tags: ["ready"]);

        services.AddMessaging(configuration, clientName: "billing-service")
            .AddOutbox<BillingDbContext>()
            .AddConsumer<BillingDbContext>(QueueName, consumer => consumer
                .Subscribe<ShipmentCreatedIntegrationEvent, ShipmentCreatedHandler>()
                .Subscribe<ShipmentDeliveredIntegrationEvent, ShipmentDeliveredHandler>()
                .Subscribe<ShipmentCancelledIntegrationEvent, ShipmentCancelledHandler>());

        return services;
    }

    public static async Task MigrateBillingDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BillingDbContext>().Database.MigrateAsync();
    }
}
