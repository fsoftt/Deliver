namespace Deliver.SharedKernel;

/// <summary>Commits every change of a use case atomically (aggregates, outbox and inbox rows).</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
