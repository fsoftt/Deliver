using RabbitMQ.Client;

namespace Deliver.Messaging.RabbitMq;

/// <summary>
/// Queue layout for one consuming service:
/// <code>
///  {exchange} --routing key--> {queue} --(handler fails)------------> {queue}.retry.N --(TTL expires)--> {queue}
///                                      --(retries exhausted/poison)--> {queue}.dlq
/// </code>
/// Delayed retries use one queue per delay tier (x-message-ttl + dead-lettering back to the main
/// queue). That avoids the head-of-line blocking of per-message TTLs and needs no broker plugin.
/// </summary>
public static class Topology
{
    public static string RetryQueue(string queue, int tier) => $"{queue}.retry.{tier + 1}";

    public static string DeadLetterQueue(string queue) => $"{queue}.dlq";

    public static Task DeclareExchangeAsync(IChannel channel, string exchange, CancellationToken cancellationToken) =>
        channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            cancellationToken: cancellationToken);

    public static async Task DeclareConsumerQueuesAsync(
        IChannel channel,
        string queue,
        IReadOnlyList<TimeSpan> retryDelays,
        IEnumerable<(string Exchange, string RoutingKey)> bindings,
        CancellationToken cancellationToken)
    {
        var deadLetterQueue = DeadLetterQueue(queue);
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);

        // Messages rejected without requeue (e.g. unreadable payloads) are routed to the DLQ by the broker.
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = deadLetterQueue,
            },
            cancellationToken: cancellationToken);

        for (var tier = 0; tier < retryDelays.Count; tier++)
        {
            await channel.QueueDeclareAsync(RetryQueue(queue, tier), durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = (int)retryDelays[tier].TotalMilliseconds,
                    ["x-dead-letter-exchange"] = "",
                    ["x-dead-letter-routing-key"] = queue,
                },
                cancellationToken: cancellationToken);
        }

        foreach (var (exchange, routingKey) in bindings.Distinct())
        {
            await DeclareExchangeAsync(channel, exchange, cancellationToken);
            await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: cancellationToken);
        }
    }
}
