using Deliver.Billing.Application.IntegrationEvents;
using Deliver.Billing.Domain.Invoices;
using Deliver.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Billing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBillingApplication(this IServiceCollection services)
    {
        services.AddScoped<PublishBillingIntegrationEvents>();
        services.AddScoped<IDomainEventHandler<DeliveryPriceCalculatedDomainEvent>>(sp => sp.GetRequiredService<PublishBillingIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<PaymentCapturedDomainEvent>>(sp => sp.GetRequiredService<PublishBillingIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<PaymentFailedDomainEvent>>(sp => sp.GetRequiredService<PublishBillingIntegrationEvents>());
        return services;
    }
}
