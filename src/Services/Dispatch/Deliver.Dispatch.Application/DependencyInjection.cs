using Deliver.Application.Pipeline;
using Deliver.Dispatch.Application.Features.Matching;
using Deliver.Dispatch.Application.IntegrationEvents;
using Deliver.Dispatch.Domain.Assignments;
using Deliver.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Dispatch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDispatchApplication(this IServiceCollection services)
    {
        services.AddApplicationPipeline(typeof(DependencyInjection).Assembly);
        services.AddScoped<DriverMatcher>();
        services.AddScoped<IDomainEventHandler<DriverAssignedDomainEvent>, PublishDispatchIntegrationEvents>();
        return services;
    }
}
