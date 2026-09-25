using Deliver.Fleet.Domain.Drivers;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deliver.Fleet.Infrastructure.Persistence;

public sealed class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Driver>(driver =>
        {
            driver.ToTable("drivers");
            driver.HasKey(d => d.Id);
            driver.Property(d => d.Id).HasConversion(id => id.Value, value => new DriverId(value)).ValueGeneratedNever();
            driver.Property(d => d.Name).HasMaxLength(100);
            driver.Property(d => d.Phone).HasMaxLength(30);
            driver.Property(d => d.Availability).HasConversion<string>().HasMaxLength(20);
            driver.Property(d => d.Version).IsRowVersion();
            driver.OwnsOne(d => d.Vehicle, vehicle =>
            {
                vehicle.Property(v => v.Plate).HasMaxLength(12);
                vehicle.Property(v => v.Type).HasConversion<string>().HasMaxLength(20);
                vehicle.Property(v => v.CapacityKg).HasPrecision(10, 2);
            });
            driver.HasIndex(d => d.Availability);
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
            throw new DomainException("The driver was modified by another operation. Please retry.");
        }
    }
}

internal sealed class FleetDbContextFactory : IDesignTimeDbContextFactory<FleetDbContext>
{
    public FleetDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FleetDbContext>()
            .UseNpgsql("Host=localhost;Database=fleet;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options);
}
