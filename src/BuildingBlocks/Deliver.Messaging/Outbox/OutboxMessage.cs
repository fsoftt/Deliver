namespace Deliver.Messaging.Outbox;

/// <summary>An integration event waiting to be published, stored in the producing service's database.</summary>
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Exchange { get; private set; } = null!;
    public string RoutingKey { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public string? CorrelationId { get; private set; }
    public string? TraceParent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    internal static OutboxMessage Create(
        string eventType,
        string exchange,
        string routingKey,
        string payload,
        string? correlationId,
        string? traceParent,
        DateTimeOffset occurredAt) => new()
    {
        // Version 7 GUIDs are time-ordered, which keeps the primary key index append-only.
        Id = Guid.CreateVersion7(occurredAt),
        EventType = eventType,
        Exchange = exchange,
        RoutingKey = routingKey,
        Payload = payload,
        CorrelationId = correlationId,
        TraceParent = traceParent,
        OccurredAt = occurredAt,
    };

    internal void MarkPublished(DateTimeOffset at)
    {
        Attempts++;
        ProcessedAt = at;
        LastError = null;
    }

    internal void RecordFailure(string error)
    {
        Attempts++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
