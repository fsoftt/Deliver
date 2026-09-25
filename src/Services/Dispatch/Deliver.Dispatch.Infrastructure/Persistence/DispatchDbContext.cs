using Deliver.Dispatch.Domain.Assignments;
using Deliver.Dispatch.Domain.Drivers;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deliver.Dispatch.Infrastructure.Persistence;

public sealed class DispatchDbContext(DbContextOptions<DispatchDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<DeliveryAssignment> Assignments => Set<DeliveryAssignment>();
    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryAssignment>(assignment =>
        {
            assignment.ToTable("delivery_assignments");
            assignment.HasKey(a => a.Id);
            assignment.Property(a => a.Id).ValueGeneratedNever();
            assignment.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            assignment.Property(a => a.RequiredCapacityKg).HasPrecision(10, 3);
            assignment.Ignore(a => a.RejectedDriverIds);
            assignment.Property<Guid[]>("_rejectedDrivers").HasColumnName("rejected_driver_ids");
            assignment.Property(a => a.Version).IsRowVersion();
            assignment.HasIndex(a => a.ShipmentId).IsUnique();
            assignment.HasIndex(a => new { a.Status, a.CreatedAt });
        });

        modelBuilder.Entity<Driver>(driver =>
        {
            driver.ToTable("drivers");
            driver.HasKey(d => d.Id);
            driver.Property(d => d.Id).ValueGeneratedNever();
            driver.Property(d => d.Name).HasMaxLength(100);
            driver.Property(d => d.CapacityKg).HasPrecision(10, 2);
            driver.Property(d => d.Version).IsRowVersion();
            driver.HasIndex(d => d.IsAvailable);
        });

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
            throw new DomainException("Dispatch data was modified concurrently. Please retry.");
        }
    }
}

internal sealed class DispatchDbContextFactory : IDesignTimeDbContextFactory<DispatchDbContext>
{
    public DispatchDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DispatchDbContext>()
            .UseNpgsql("Host=localhost;Database=dispatch;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options);
}
