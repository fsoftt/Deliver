using System.Globalization;
using System.Text;
using System.Text.Json;
using Deliver.Messaging.Inbox;
using Deliver.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Deliver.Messaging.Consuming;

/// <summary>
/// Hosts one queue of a service. For every delivery:
/// <list type="number">
/// <item>Opens a DB transaction and checks the inbox: already processed by this consumer? → ack and skip.</item>
/// <item>Runs the handler, records the message in the inbox and commits, all atomically, so the
/// business change and the "processed" marker can never diverge.</item>
/// <item>On failure: republishes to the next delayed retry queue, or to the DLQ once retries are exhausted.</item>
/// </list>
/// Messages are acked only after they are safely handled or safely moved to another queue.
/// </summary>
internal sealed class RabbitMqConsumer<TContext>(
    string queue,
    IReadOnlyList<Subscription> subscriptions,
    RabbitMqConnection connection,
    RabbitMqPublisher publisher,
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    TimeProvider clock,
    ILogger<RabbitMqConsumer<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private readonly Dictionary<string, Subscription> _subscriptionsByType =
        subscriptions.ToDictionary(s => s.EventType);

    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelays = options.Value.EffectiveRetryDelays;
        var conn = await connection.GetAsync(stoppingToken);

        _channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);
        await Topology.DeclareConsumerQueuesAsync(
            _channel, queue, retryDelays,
            subscriptions.Select(s => (s.Exchange, s.RoutingKey)),
            stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: options.Value.PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(_channel, delivery, retryDelays, stoppingToken);
        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer, stoppingToken);

        logger.LogInformation("Consuming {Queue} ({EventTypes})", queue, string.Join(", ", _subscriptionsByType.Keys));
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task OnReceivedAsync(
        IChannel channel, BasicDeliverEventArgs delivery, TimeSpan[] retryDelays, CancellationToken stoppingToken)
    {
        var body = delivery.Body.ToArray();
        var retryCount = ReadIntHeader(delivery.BasicProperties.Headers, MessagingDiagnostics.RetryCountHeader);

        MessageEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<MessageEnvelope>(body, MessagingJson.Options);
        }
        catch (JsonException ex)
        {
            envelope = null;
            logger.LogError(ex, "Unreadable message on {Queue}", queue);
        }

        if (envelope is null)
        {
            // Poison message: the queue's dead-letter settings route it to the DLQ.
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            return;
        }

        if (!_subscriptionsByType.TryGetValue(envelope.EventType, out var subscription))
        {
            logger.LogWarning("No handler for {EventType} on {Queue}; acknowledging", envelope.EventType, queue);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        var traceParent = ReadStringHeader(delivery.BasicProperties.Headers, MessagingDiagnostics.TraceParentHeader);
        using var activity = MessagingDiagnostics.StartProcess(queue, envelope, traceParent);
        CorrelationContext.Current = envelope.CorrelationId;
        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["MessageId"] = envelope.MessageId,
            ["EventType"] = envelope.EventType,
        });

        try
        {
            var context = new MessageContext(envelope.MessageId, envelope.CorrelationId, envelope.OccurredAt, retryCount + 1);
            await ProcessAsync(envelope, subscription, context, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down: leave the message unacked; the broker redelivers it.
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            await HandleFailureAsync(channel, delivery, body, envelope, retryCount, retryDelays, ex, stoppingToken);
        }
        finally
        {
            CorrelationContext.Current = null;
        }
    }

    private async Task ProcessAsync(
        MessageEnvelope envelope, Subscription subscription, MessageContext context, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var inbox = db.Set<ProcessedMessage>();
        var alreadyProcessed = await inbox.AnyAsync(
            m => m.MessageId == envelope.MessageId && m.Consumer == subscription.ConsumerName, cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Duplicate {EventType} {MessageId} ignored by {Consumer}",
                envelope.EventType, envelope.MessageId, subscription.ConsumerName);
            return;
        }

        await subscription.InvokeAsync(scope.ServiceProvider, envelope.Payload, context, cancellationToken);

        inbox.Add(new ProcessedMessage(envelope.MessageId, subscription.ConsumerName, clock.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Handled {EventType} {MessageId} with {Consumer} (attempt {Attempt})",
            envelope.EventType, envelope.MessageId, subscription.ConsumerName, context.DeliveryAttempt);
    }

    private async Task HandleFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        byte[] body,
        MessageEnvelope envelope,
        int retryCount,
        TimeSpan[] retryDelays,
        Exception exception,
        CancellationToken stoppingToken)
    {
        var headers = delivery.BasicProperties.Headers is { } existing
            ? new Dictionary<string, object?>(existing)
            : new Dictionary<string, object?>();

        string targetQueue;
        if (retryCount < retryDelays.Length)
        {
            targetQueue = Topology.RetryQueue(queue, retryCount);
            headers[MessagingDiagnostics.RetryCountHeader] = retryCount + 1;
            logger.LogWarning(exception,
                "{EventType} {MessageId} failed (attempt {Attempt}); retrying in {Delay}",
                envelope.EventType, envelope.MessageId, retryCount + 1, retryDelays[retryCount]);
        }
        else
        {
            targetQueue = Topology.DeadLetterQueue(queue);
            headers["x-exception-type"] = exception.GetType().FullName;
            headers["x-exception-message"] = exception.Message;
            headers["x-failed-at"] = clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
            logger.LogError(exception,
                "{EventType} {MessageId} failed permanently after {Attempts} attempts; moved to {DeadLetterQueue}",
                envelope.EventType, envelope.MessageId, retryCount + 1, targetQueue);
        }

        try
        {
            var properties = new BasicProperties(delivery.BasicProperties) { Headers = headers };
            await publisher.PublishAsync(exchange: "", routingKey: targetQueue, properties, body, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception publishException) when (publishException is not OperationCanceledException)
        {
            // Could not park the message elsewhere: put it back on the queue rather than lose it.
            logger.LogError(publishException, "Could not move {MessageId} to {Queue}; requeueing", envelope.MessageId, targetQueue);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);
        }
    }

    private static int ReadIntHeader(IDictionary<string, object?>? headers, string key) =>
        headers is not null && headers.TryGetValue(key, out var value)
            ? value switch
            {
                int i => i,
                long l => (int)l,
                byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
                _ => 0,
            }
            : 0;

    private static string? ReadStringHeader(IDictionary<string, object?>? headers, string key) =>
        headers is not null && headers.TryGetValue(key, out var value)
            ? value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                _ => null,
            }
            : null;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
            await _channel.DisposeAsync();
    }
}
