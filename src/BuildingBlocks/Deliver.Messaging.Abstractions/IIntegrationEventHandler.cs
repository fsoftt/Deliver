namespace Deliver.Messaging;

/// <summary>
/// Handles an integration event consumed from the broker. Implementations must be safe to run
/// inside a transaction that also records the message as processed (idempotent consumer).
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);
}

/// <param name="MessageId">Unique id of the message; the idempotency key.</param>
/// <param name="CorrelationId">Id shared by every message that belongs to the same business flow.</param>
/// <param name="OccurredAt">When the producer recorded the event.</param>
/// <param name="DeliveryAttempt">1 for the first delivery, incremented on each retry.</param>
public sealed record MessageContext(Guid MessageId, string? CorrelationId, DateTimeOffset OccurredAt, int DeliveryAttempt);
