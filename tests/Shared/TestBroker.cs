using System.Text;
using System.Text.Json;
using Deliver.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Deliver.Testing;

/// <summary>Plays the role of "the other services" in integration tests: publishes their events and observes ours.</summary>
public sealed class TestBroker : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private TestBroker(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<TestBroker> ConnectAsync(string connectionString)
    {
        var connection = await new ConnectionFactory { Uri = new Uri(connectionString) }.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        return new TestBroker(connection, channel);
    }

    /// <summary>Publishes an event exactly as a producing service's outbox would.</summary>
    public async Task<Guid> PublishAsync<TEvent>(TEvent integrationEvent, Guid? messageId = null, string? correlationId = null)
        where TEvent : IIntegrationEvent
    {
        var id = messageId ?? Guid.NewGuid();
        var envelope = new MessageEnvelope(
            id,
            IntegrationEventNames.EventTypeOf(typeof(TEvent)),
            DateTimeOffset.UtcNow,
            correlationId ?? Guid.NewGuid().ToString(),
            JsonSerializer.SerializeToElement(integrationEvent, MessagingJson.Options));

        await _channel.ExchangeDeclareAsync(TEvent.Exchange, ExchangeType.Topic, durable: true);
        await _channel.BasicPublishAsync(
            TEvent.Exchange,
            TEvent.RoutingKey,
            mandatory: false,
            new BasicProperties { MessageId = id.ToString(), ContentType = "application/json", Persistent = true },
            JsonSerializer.SerializeToUtf8Bytes(envelope, MessagingJson.Options));
        return id;
    }

    public async Task PublishRawAsync(string queue, string body) =>
        await _channel.BasicPublishAsync("", queue, Encoding.UTF8.GetBytes(body));

    /// <summary>Creates a private queue that receives a copy of the given events (fan-out observer).</summary>
    public async Task<string> ObserveAsync(string exchange, string routingKey)
    {
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);
        // RabbitMQ 4 only allows transient queues when they are exclusive to the connection.
        var queue = (await _channel.QueueDeclareAsync($"test-observer-{Guid.NewGuid():N}", durable: false, exclusive: true, autoDelete: true)).QueueName;
        await _channel.QueueBindAsync(queue, exchange, routingKey);
        return queue;
    }

    public async Task<ReceivedMessage> ReceiveAsync(string queue, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(20));
        while (DateTime.UtcNow < deadline)
        {
            var result = await _channel.BasicGetAsync(queue, autoAck: true);
            if (result is not null)
            {
                var body = Encoding.UTF8.GetString(result.Body.Span);
                return new ReceivedMessage(TryReadEnvelope(body), result.BasicProperties.Headers ?? new Dictionary<string, object?>(), body);
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"No message arrived on {queue}.");
    }

    private static MessageEnvelope? TryReadEnvelope(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<MessageEnvelope>(body, MessagingJson.Options);
        }
        catch (JsonException)
        {
            return null; // e.g. a poison message that was dead-lettered on purpose
        }
    }

    public async Task<uint> CountAsync(string queue) => (await _channel.QueueDeclarePassiveAsync(queue)).MessageCount;

    /// <summary>Waits until a service under test has declared (and bound) its queue.</summary>
    public async Task WaitForQueueAsync(string queue, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (true)
        {
            await using var probe = await _connection.CreateChannelAsync();
            try
            {
                await probe.QueueDeclarePassiveAsync(queue);
                await Task.Delay(300); // bindings are declared right after the queue
                return;
            }
            catch (OperationInterruptedException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(200);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public sealed record ReceivedMessage(MessageEnvelope? Envelope, IDictionary<string, object?> Headers, string RawBody)
{
    public string? Header(string key) => Headers.TryGetValue(key, out var value)
        ? value switch { byte[] bytes => Encoding.UTF8.GetString(bytes), null => null, _ => value.ToString() }
        : null;
}
