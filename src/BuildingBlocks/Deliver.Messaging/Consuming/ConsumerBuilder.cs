using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deliver.Messaging.Consuming;

internal sealed record Subscription(
    string Exchange,
    string RoutingKey,
    string EventType,
    string ConsumerName,
    Func<IServiceProvider, JsonElement, MessageContext, CancellationToken, Task> InvokeAsync);

/// <summary>Declares which integration events a service's queue receives and who handles them.</summary>
public sealed class ConsumerBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Subscription> _subscriptions = [];

    internal ConsumerBuilder(IServiceCollection services) => _services = services;

    internal IReadOnlyList<Subscription> Subscriptions => _subscriptions;

    public ConsumerBuilder Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _services.TryAddScoped<THandler>();

        _subscriptions.Add(new Subscription(
            TEvent.Exchange,
            TEvent.RoutingKey,
            IntegrationEventNames.EventTypeOf(typeof(TEvent)),
            ConsumerName: typeof(THandler).Name,
            InvokeAsync: (serviceProvider, payload, context, cancellationToken) =>
            {
                var integrationEvent = payload.Deserialize<TEvent>(MessagingJson.Options)
                    ?? throw new JsonException($"Payload of {typeof(TEvent).Name} is null.");

                return serviceProvider.GetRequiredService<THandler>()
                    .HandleAsync(integrationEvent, context, cancellationToken);
            }));

        return this;
    }
}
