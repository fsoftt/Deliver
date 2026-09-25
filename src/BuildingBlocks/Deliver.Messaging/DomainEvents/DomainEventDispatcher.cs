using System.Collections.Concurrent;
using System.Reflection;
using Deliver.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Deliver.Messaging.DomainEvents;

/// <summary>In-process dispatch of domain events to <see cref="IDomainEventHandler{TEvent}"/> implementations.</summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider)
{
    private delegate Task Invoker(IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken);

    private static readonly ConcurrentDictionary<Type, Invoker> Invokers = new();

    private static readonly MethodInfo InvokeHandlersMethod =
        typeof(DomainEventDispatcher).GetMethod(nameof(InvokeHandlersAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var invoker = Invokers.GetOrAdd(domainEvent.GetType(),
            type => InvokeHandlersMethod.MakeGenericMethod(type).CreateDelegate<Invoker>());

        return invoker(serviceProvider, domainEvent, cancellationToken);
    }

    private static async Task InvokeHandlersAsync<TEvent>(
        IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
            await handler.HandleAsync((TEvent)domainEvent, cancellationToken);
    }
}
