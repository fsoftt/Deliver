using Deliver.Messaging;
using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Shipping.Infrastructure.Persistence;

/// <summary>The Shipping service's own database. No other service connects to it.</summary>
public sealed class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShippingDbContext).Assembly);
        modelBuilder.AddOutboxMessages();
        modelBuilder.AddInboxMessages();
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DomainException("The shipment was modified by another operation. Please retry.");
        }
    }
}
