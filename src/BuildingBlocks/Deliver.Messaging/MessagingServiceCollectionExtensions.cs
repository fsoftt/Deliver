using Deliver.Messaging.Consuming;
using Deliver.Messaging.DomainEvents;
using Deliver.Messaging.Outbox;
using Deliver.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Deliver.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Registers the RabbitMQ connection, publisher and the domain event → outbox bridge.</summary>
    public static MessagingBuilder AddMessaging(this IServiceCollection services, IConfiguration configuration, string clientName)
    {
        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Configure(o =>
            {
                o.ClientName = clientName;
                o.ConnectionString = configuration.GetConnectionString("RabbitMq") ?? o.ConnectionString;
            });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqPublisher>();
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<DomainEventsInterceptor>();
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

        return new MessagingBuilder(services);
    }

    /// <summary>Hooks domain event dispatching into the DbContext's SaveChangesAsync.</summary>
    public static DbContextOptionsBuilder UseDomainEvents(this DbContextOptionsBuilder options, IServiceProvider serviceProvider) =>
        options.AddInterceptors(serviceProvider.GetRequiredService<DomainEventsInterceptor>());
}

public sealed class MessagingBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;

    /// <summary>Stores integration events in <typeparamref name="TContext"/> and publishes them in the background.</summary>
    public MessagingBuilder AddOutbox<TContext>()
        where TContext : DbContext
    {
        Services.AddScoped<IOutbox, EfOutbox<TContext>>();
        Services.AddHostedService<OutboxProcessor<TContext>>();
        return this;
    }

    /// <summary>Consumes <paramref name="queue"/>; the inbox lives in <typeparamref name="TContext"/>.</summary>
    public MessagingBuilder AddConsumer<TContext>(string queue, Action<ConsumerBuilder> configure)
        where TContext : DbContext
    {
        var builder = new ConsumerBuilder(Services);
        configure(builder);
        var subscriptions = builder.Subscriptions;

        Services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp => new RabbitMqConsumer<TContext>(
            queue,
            subscriptions,
            sp.GetRequiredService<RabbitMqConnection>(),
            sp.GetRequiredService<RabbitMqPublisher>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<MessagingOptions>>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<ILogger<RabbitMqConsumer<TContext>>>()));
        return this;
    }
}
