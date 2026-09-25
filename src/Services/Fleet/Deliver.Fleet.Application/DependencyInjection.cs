using Deliver.Fleet.Application.Features.ManageDriver;
using Deliver.Fleet.Application.Features.RegisterDriver;
using Deliver.Fleet.Application.IntegrationEvents;
using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Fleet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFleetApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterDriverHandler>();
        services.AddScoped<ChangeAvailabilityHandler>();
        services.AddScoped<AssignVehicleHandler>();

        services.AddScoped<PublishFleetIntegrationEvents>();
        services.AddScoped<IDomainEventHandler<DriverBecameAvailableDomainEvent>>(sp => sp.GetRequiredService<PublishFleetIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<DriverBecameUnavailableDomainEvent>>(sp => sp.GetRequiredService<PublishFleetIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<DriverAssignmentRejectedDomainEvent>>(sp => sp.GetRequiredService<PublishFleetIntegrationEvents>());

        return services;
    }
}
