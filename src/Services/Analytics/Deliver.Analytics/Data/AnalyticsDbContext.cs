using Deliver.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deliver.Analytics.Data;

public sealed class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<ShipmentFact> ShipmentFacts => Set<ShipmentFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShipmentFact>(fact =>
        {
            fact.ToTable("shipment_facts");
            fact.HasKey(f => f.ShipmentId);
            fact.Property(f => f.ShipmentId).ValueGeneratedNever();
            fact.Property(f => f.Status).HasMaxLength(20);
            fact.Property(f => f.Revenue).HasPrecision(12, 2);
            fact.Property(f => f.Currency).HasMaxLength(3);
            fact.HasIndex(f => f.Status);
            fact.HasIndex(f => f.CreatedOn);
        });

        modelBuilder.AddInboxMessages();
    }

    /// <summary>Loads the fact for a shipment, creating it if this is the first event seen for it.</summary>
    public async Task<ShipmentFact> GetOrCreateFactAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var fact = await ShipmentFacts.FirstOrDefaultAsync(f => f.ShipmentId == shipmentId, cancellationToken);
        if (fact is null)
        {
            fact = ShipmentFact.For(shipmentId);
            ShipmentFacts.Add(fact);
        }

        return fact;
    }
}

internal sealed class AnalyticsDbContextFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    public AnalyticsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql("Host=localhost;Database=analytics;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options);
}
