using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Deliver.Messaging.RabbitMq;

/// <summary>
/// One long-lived connection per service (connections are expensive, channels are cheap).
/// Waits for the broker at startup; afterwards the client's automatic recovery takes over.
/// </summary>
public sealed class RabbitMqConnection(IOptions<MessagingOptions> options, ILogger<RabbitMqConnection> logger)
    : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public bool IsOpen => _connection?.IsOpen ?? false;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            while (_connection is null)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        Uri = new Uri(options.Value.ConnectionString),
                        ClientProvidedName = options.Value.ClientName,
                        AutomaticRecoveryEnabled = true,
                        TopologyRecoveryEnabled = true,
                    };
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    logger.LogInformation("Connected to RabbitMQ as {ClientName}", options.Value.ClientName);
                }
                catch (BrokerUnreachableException ex)
                {
                    logger.LogWarning(ex, "RabbitMQ is not reachable yet, retrying in 3 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                }
            }

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _lock.Dispose();
    }
}
