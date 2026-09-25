namespace Deliver.Messaging;

/// <summary>
/// Stores an integration event in the same database transaction as the business change.
/// A background processor publishes it later (Transactional Outbox pattern).
/// </summary>
public interface IOutbox
{
    void Enqueue<TEvent>(TEvent integrationEvent)
        where TEvent : IIntegrationEvent;
}
