using System.Text.Json;

namespace Deliver.Messaging;

/// <summary>The single wire format every service publishes and consumes.</summary>
public sealed record MessageEnvelope(
    Guid MessageId,
    string EventType,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    JsonElement Payload);

public static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
