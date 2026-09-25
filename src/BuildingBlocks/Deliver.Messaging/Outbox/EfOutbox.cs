using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Messaging.Outbox;

/// <summary>
/// Adds the event to the current DbContext. It is persisted by the same <c>SaveChangesAsync</c>
/// (and therefore the same transaction) as the aggregate that caused it.
/// </summary>
internal sealed class EfOutbox<TContext>(TContext dbContext, TimeProvider clock) : IOutbox
    where TContext : DbContext
{
    public void Enqueue<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(
            eventType: IntegrationEventNames.EventTypeOf(typeof(TEvent)),
            exchange: TEvent.Exchange,
            routingKey: TEvent.RoutingKey,
            payload: JsonSerializer.Serialize(integrationEvent, MessagingJson.Options),
            correlationId: CorrelationContext.Current,
            traceParent: Activity.Current?.Id,
            occurredAt: clock.GetUtcNow());

        dbContext.Set<OutboxMessage>().Add(message);
    }
}
