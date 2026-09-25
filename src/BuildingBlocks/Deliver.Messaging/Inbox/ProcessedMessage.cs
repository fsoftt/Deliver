namespace Deliver.Messaging.Inbox;

/// <summary>
/// Records that a consumer has already handled a message. The composite key
/// (MessageId, Consumer) makes duplicate processing impossible even under concurrency.
/// </summary>
public sealed class ProcessedMessage
{
    private ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid messageId, string consumer, DateTimeOffset processedAt)
    {
        MessageId = messageId;
        Consumer = consumer;
        ProcessedAt = processedAt;
    }

    public Guid MessageId { get; private set; }
    public string Consumer { get; private set; } = null!;
    public DateTimeOffset ProcessedAt { get; private set; }
}
