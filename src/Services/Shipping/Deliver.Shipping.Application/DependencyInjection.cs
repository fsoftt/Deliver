using Deliver.SharedKernel;
using Deliver.Shipping.Application.Features.CreateShipment;
using Deliver.Shipping.Application.Features.ShipmentLifecycle;
using Deliver.Shipping.Application.IntegrationEvents;
using Deliver.Shipping.Domain.Shipments;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Shipping.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShippingApplication(this IServiceCollection services)
    {
        // Use cases: plain classes, no mediator needed for this size of service.
        services.AddScoped<CreateShipmentHandler>();
        services.AddScoped<PickUpShipmentHandler>();
        services.AddScoped<StartTransitHandler>();
        services.AddScoped<DeliverShipmentHandler>();
        services.AddScoped<CancelShipmentHandler>();

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
