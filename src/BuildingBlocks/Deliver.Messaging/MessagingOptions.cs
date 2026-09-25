namespace Deliver.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    private static readonly TimeSpan[] DefaultRetryDelays =
        [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];

    /// <summary>AMQP URI. Bound from <c>ConnectionStrings:RabbitMq</c>.</summary>
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";

    /// <summary>Name shown in the RabbitMQ management UI for this service's connection.</summary>
    public string ClientName { get; set; } = "deliver";

    /// <summary>
    /// Delay of each retry tier. The number of entries is the number of retries before a
    /// message is dead-lettered. Defaults to 2s, 10s, 30s.
    /// </summary>
    public TimeSpan[]? RetryDelays { get; set; }

    public ushort PrefetchCount { get; set; } = 16;

    public TimeSpan OutboxPollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public int OutboxBatchSize { get; set; } = 50;

    internal TimeSpan[] EffectiveRetryDelays => RetryDelays is { Length: > 0 } ? RetryDelays : DefaultRetryDelays;
}
