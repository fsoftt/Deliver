using Deliver.Application.Pipeline;
using Deliver.SharedKernel;
using Deliver.Shipping.Application.IntegrationEvents;
using Deliver.Shipping.Domain.Shipments;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Shipping.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        // Use cases (commands/queries) + validators, behind the logging and validation pipeline.
        services.AddApplicationPipeline(typeof(DependencyInjection).Assembly);

        // Domain event → integration event translation (runs inside the SaveChanges transaction).
        services.AddScoped<PublishShipmentIntegrationEvents>();
        services.AddScoped<IDomainEventHandler<ShipmentCreatedDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<DriverAssignedDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<ShipmentPickedUpDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<ShipmentInTransitDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<ShipmentDeliveredDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<ShipmentCancelledDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<ShipmentDeliveryPaymentFailedDomainEvent>>(sp => sp.GetRequiredService<PublishShipmentIntegrationEvents>());

        return services;
    }
}
