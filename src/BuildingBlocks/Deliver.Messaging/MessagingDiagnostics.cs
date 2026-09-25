using System.Diagnostics;

namespace Deliver.Messaging;

/// <summary>
/// Tracing for the asynchronous hops. The W3C <c>traceparent</c> is stored in the outbox row and in the
/// AMQP headers, so one trace spans HTTP request → outbox → RabbitMQ → consumer → next outbox → ...
/// </summary>
public static class MessagingDiagnostics
{
    public const string ActivitySourceName = "Deliver.Messaging";
    public const string TraceParentHeader = "traceparent";
    public const string RetryCountHeader = "x-retry-count";

    internal static readonly ActivitySource Source = new(ActivitySourceName);

    internal static Activity? StartPublish(string exchange, string routingKey, Guid messageId, string? parentTraceId)
    {
        var activity = Source.StartActivity($"{routingKey} publish", ActivityKind.Producer, parentTraceId);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "publish");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", messageId.ToString());
        return activity;
    }

    internal static Activity? StartProcess(string queue, MessageEnvelope envelope, string? parentTraceId)
    {
        var activity = Source.StartActivity($"{envelope.EventType} process", ActivityKind.Consumer, parentTraceId);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.message.id", envelope.MessageId.ToString());
        activity?.SetTag("messaging.message.conversation_id", envelope.CorrelationId);
        return activity;
    }
}
