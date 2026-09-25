using System.Globalization;
using System.Text;
using System.Text.Json;
using Deliver.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Deliver.Messaging.Outbox;

/// <summary>
/// Polls the outbox table and publishes pending messages in order.
/// <list type="bullet">
/// <item><c>FOR UPDATE SKIP LOCKED</c> lets several instances of a service share the work without
/// publishing the same row twice concurrently.</item>
/// <item>A message is marked as published only after the broker confirms it. A crash between the
/// confirm and the commit re-publishes the message, so delivery is at-least-once and
/// consumers must be idempotent.</item>
/// <item>A failure stops the batch (keeps ordering) and is retried on the next poll.</item>
/// </list>
/// </summary>
internal sealed class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    RabbitMqPublisher publisher,
    IOptions<MessagingOptions> options,
    TimeProvider clock,
    ILogger<OutboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.OutboxPollingInterval);
        do
        {
            try
            {
                int published;
                do
                {
                    published = await PublishBatchAsync(stoppingToken);
                }
                while (published == options.Value.OutboxBatchSize && !stoppingToken.IsCancellationRequested);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Outbox processing failed; will retry on next poll");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var messages = await db.Set<OutboxMessage>()
            .FromSqlRaw(
                """
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL
                ORDER BY occurred_at, id
                LIMIT {0}
                FOR UPDATE SKIP LOCKED
                """,
                options.Value.OutboxBatchSize)
            .ToListAsync(cancellationToken);

        var published = 0;
        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(message, cancellationToken);
                message.MarkPublished(clock.GetUtcNow());
                published++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RecordFailure(ex.Message);
                logger.LogWarning(ex, "Failed to publish outbox message {MessageId} ({EventType}), attempt {Attempt}",
                    message.Id, message.EventType, message.Attempts);
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return published;
    }

    private async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var activity = MessagingDiagnostics.StartPublish(message.Exchange, message.RoutingKey, message.Id, message.TraceParent);

        var envelope = new MessageEnvelope(
            message.Id,
            message.EventType,
            message.OccurredAt,
            message.CorrelationId,
            JsonSerializer.Deserialize<JsonElement>(message.Payload));

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            CorrelationId = message.CorrelationId,
            Type = message.EventType,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                [MessagingDiagnostics.TraceParentHeader] = activity?.Id ?? message.TraceParent,
                ["x-occurred-at"] = message.OccurredAt.ToString("O", CultureInfo.InvariantCulture),
            },
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, MessagingJson.Options));
        await publisher.PublishAsync(message.Exchange, message.RoutingKey, properties, body, cancellationToken);

        logger.LogInformation("Published {EventType} {MessageId} to {Exchange}/{RoutingKey}",
            message.EventType, message.Id, message.Exchange, message.RoutingKey);
    }
}
