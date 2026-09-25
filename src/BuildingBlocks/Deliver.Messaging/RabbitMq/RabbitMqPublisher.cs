using RabbitMQ.Client;

namespace Deliver.Messaging.RabbitMq;

/// <summary>
/// Publishes with publisher confirms: <see cref="PublishAsync"/> only completes once the broker has
/// taken responsibility for the message, so the outbox never marks an unconfirmed message as sent.
/// Channels are not thread-safe, hence the lock around the single publishing channel.
/// </summary>
public sealed class RabbitMqPublisher(RabbitMqConnection connection) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _declaredExchanges = [];
    private IChannel? _channel;

    public async Task PublishAsync(
        string exchange,
        string routingKey,
        BasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            // The default exchange ("") is used to address a queue directly (retries, dead letters).
            if (exchange.Length > 0 && _declaredExchanges.Add(exchange))
                await Topology.DeclareExchangeAsync(channel, exchange, cancellationToken);

            await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        if (_channel is not null)
            await _channel.DisposeAsync();

        _declaredExchanges.Clear();
        var conn = await connection.GetAsync(cancellationToken);
        _channel = await conn.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        _lock.Dispose();
    }
}
