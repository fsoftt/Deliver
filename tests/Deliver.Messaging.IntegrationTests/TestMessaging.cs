using System.Collections.Concurrent;
using Deliver.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Messaging.IntegrationTests;

public sealed record PingIntegrationEvent(Guid Id) : IIntegrationEvent
{
    public static string Exchange => "test.events";
    public static string RoutingKey => "test.ping";
}

public sealed record PongIntegrationEvent(Guid Id) : IIntegrationEvent
{
    public static string Exchange => "test.events";
    public static string RoutingKey => "test.pong";
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutboxMessages();
        modelBuilder.AddInboxMessages();
    }
}

/// <summary>Records handler invocations and makes the handler fail a configurable number of times.</summary>
public sealed class HandlerProbe
{
    private readonly ConcurrentDictionary<Guid, int> _calls = new();
    private readonly ConcurrentDictionary<Guid, int> _failuresToSimulate = new();

    public void FailTimes(Guid id, int times) => _failuresToSimulate[id] = times;

    public int Calls(Guid id) => _calls.GetValueOrDefault(id);

    public void Invoke(Guid id)
    {
        var call = _calls.AddOrUpdate(id, 1, (_, n) => n + 1);
        if (call <= _failuresToSimulate.GetValueOrDefault(id))
            throw new InvalidOperationException($"Simulated failure #{call}");
    }
}

public sealed class PingHandler(HandlerProbe probe) : IIntegrationEventHandler<PingIntegrationEvent>
{
    public Task HandleAsync(PingIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        probe.Invoke(integrationEvent.Id);
        return Task.CompletedTask;
    }
}
