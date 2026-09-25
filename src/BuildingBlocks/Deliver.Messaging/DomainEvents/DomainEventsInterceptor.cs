using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Deliver.Messaging.DomainEvents;

/// <summary>
/// Just before <c>SaveChangesAsync</c> commits, collects the domain events raised by tracked aggregates
/// and dispatches them. Handlers typically translate them into integration events via <see cref="IOutbox"/>,
/// so business state and outgoing messages are committed in ONE transaction.
/// </summary>
public sealed class DomainEventsInterceptor(DomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);

        return result;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
        throw new InvalidOperationException("Use SaveChangesAsync so domain events can be dispatched.");

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Loop because a handler may modify another aggregate that raises further events.
        while (true)
        {
            var aggregates = context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            if (aggregates.Count == 0)
                return;

            var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
            aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
                await dispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
