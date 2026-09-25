using Deliver.Application.Pipeline;
using Deliver.Fleet.Application.IntegrationEvents;
using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Fleet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFleetApplication(this IServiceCollection services)
    {
        services.AddApplicationPipeline(typeof(DependencyInjection).Assembly);

        services.AddScoped<PublishFleetIntegrationEvents>();
        services.AddScoped<IDomainEventHandler<DriverBecameAvailableDomainEvent>>(sp => sp.GetRequiredService<PublishFleetIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<DriverBecameUnavailableDomainEvent>>(sp => sp.GetRequiredService<PublishFleetIntegrationEvents>());
        services.AddScoped<IDomainEventHandler<DriverAssignmentRejectedDomainEvent>>(sp => sp.GetRequiredService<PublishFleetIntegrationEvents>());

        return services;
    }
}
