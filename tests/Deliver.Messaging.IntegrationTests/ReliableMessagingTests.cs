using Deliver.Messaging.Inbox;
using Deliver.Messaging.Outbox;
using Deliver.Messaging.RabbitMq;
using Deliver.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: AssemblyFixture(typeof(InfrastructureFixture))]

namespace Deliver.Messaging.IntegrationTests;

/// <summary>Retry, dead-lettering, idempotency and outbox behaviour against real RabbitMQ and PostgreSQL.</summary>
public sealed class ReliableMessagingTests(InfrastructureFixture infrastructure) : IAsyncLifetime
{
    private readonly HandlerProbe _probe = new();
    private readonly string _queue = $"it-{Guid.NewGuid():N}";
    private TestBroker _broker = null!;
    private IHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        _broker = await TestBroker.ConnectAsync(infrastructure.RabbitMqConnectionString);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:RabbitMq"] = infrastructure.RabbitMqConnectionString,
            ["Messaging:RetryDelays:0"] = "00:00:00.300",
            ["Messaging:RetryDelays:1"] = "00:00:00.300",
            ["Messaging:OutboxPollingInterval"] = "00:00:00.200",
        });
        builder.Services.AddDbContext<TestDbContext>(options => options
            .UseNpgsql(infrastructure.DatabaseConnectionString("messaging_tests"))
            .UseSnakeCaseNamingConvention());
        builder.Services.AddSingleton(_probe);
        builder.Services.AddMessaging(builder.Configuration, clientName: "messaging-tests")
            .AddOutbox<TestDbContext>()
            .AddConsumer<TestDbContext>(_queue, consumer => consumer.Subscribe<PingIntegrationEvent, PingHandler>());

        _host = builder.Build();
        await using (var scope = _host.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync();

        await _host.StartAsync();
        await _broker.WaitForQueueAsync(_queue);
    }

    [Fact]
    public async Task A_transient_failure_is_retried_with_delay_and_then_processed_once()
    {
        var id = Guid.NewGuid();
        _probe.FailTimes(id, 2);

        var messageId = await _broker.PublishAsync(new PingIntegrationEvent(id));

        await Eventually.UntilAsync(() => IsProcessedAsync(messageId));
        _probe.Calls(id).ShouldBe(3);
        (await _broker.CountAsync(Topology.DeadLetterQueue(_queue))).ShouldBe(0u);
    }

    [Fact]
    public async Task A_message_that_keeps_failing_ends_in_the_dead_letter_queue()
    {
        var id = Guid.NewGuid();
        _probe.FailTimes(id, int.MaxValue);

        var messageId = await _broker.PublishAsync(new PingIntegrationEvent(id));

        var deadLetter = await _broker.ReceiveAsync(Topology.DeadLetterQueue(_queue));
        deadLetter.Envelope!.MessageId.ShouldBe(messageId);
        deadLetter.Header("x-retry-count").ShouldBe("2");
        deadLetter.Header("x-exception-type").ShouldBe(typeof(InvalidOperationException).FullName);
        _probe.Calls(id).ShouldBe(3); // first attempt + 2 retries
        (await IsProcessedAsync(messageId)).ShouldBeFalse();
    }

    [Fact]
    public async Task A_duplicated_message_is_handled_only_once()
    {
        var id = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        await _broker.PublishAsync(new PingIntegrationEvent(id), messageId);
        await _broker.PublishAsync(new PingIntegrationEvent(id), messageId);

        await Eventually.UntilAsync(() => IsProcessedAsync(messageId));
        await Task.Delay(TimeSpan.FromSeconds(1)); // leave time for the duplicate to be (not) processed
        _probe.Calls(id).ShouldBe(1);
    }

    [Fact]
    public async Task An_unreadable_message_is_dead_lettered_immediately()
    {
        await _broker.PublishRawAsync(_queue, "this is not an envelope");

        var deadLetter = await _broker.ReceiveAsync(Topology.DeadLetterQueue(_queue));
        deadLetter.RawBody.ShouldBe("this is not an envelope");
    }

    [Fact]
    public async Task Outbox_messages_are_published_after_the_transaction_commits()
    {
        var observer = await _broker.ObserveAsync(PongIntegrationEvent.Exchange, PongIntegrationEvent.RoutingKey);
        var id = Guid.NewGuid();

        await using (var scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOutbox>().Enqueue(new PongIntegrationEvent(id));
            await scope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();
        }

        var received = await _broker.ReceiveAsync(observer);
        received.Envelope!.EventType.ShouldBe("Pong");
        received.Envelope.Payload.GetProperty("id").GetGuid().ShouldBe(id);

        await using var verify = _host.Services.CreateAsyncScope();
        var outboxRow = await verify.ServiceProvider.GetRequiredService<TestDbContext>()
            .Set<OutboxMessage>().SingleAsync(m => m.Id == received.Envelope.MessageId);
        outboxRow.ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Nothing_is_published_when_the_business_transaction_is_not_committed()
    {
        var observer = await _broker.ObserveAsync(PongIntegrationEvent.Exchange, PongIntegrationEvent.RoutingKey);

        await using (var scope = _host.Services.CreateAsyncScope())
            scope.ServiceProvider.GetRequiredService<IOutbox>().Enqueue(new PongIntegrationEvent(Guid.NewGuid()));
        // Scope disposed without SaveChanges: the "business transaction" failed.

        await Task.Delay(TimeSpan.FromSeconds(1.5));
        (await _broker.CountAsync(observer)).ShouldBe(0u);
    }

    private async Task<bool> IsProcessedAsync(Guid messageId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<TestDbContext>()
            .Set<ProcessedMessage>().AnyAsync(m => m.MessageId == messageId);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _broker.DisposeAsync();
    }
}

