using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Application.Pipeline;

public static class ApplicationPipelineExtensions
{
    /// <summary>
    /// Registers the use cases (MediatR request handlers) and validators of an application assembly,
    /// wrapped in the pipeline: Logging → Validation → Handler.
    /// </summary>
    public static IServiceCollection AddApplicationPipeline(this IServiceCollection services, Assembly applicationAssembly)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);
        return services;
    }
}
