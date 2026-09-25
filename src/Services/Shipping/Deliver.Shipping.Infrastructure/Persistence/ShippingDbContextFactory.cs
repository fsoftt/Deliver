using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deliver.Shipping.Infrastructure.Persistence;

/// <summary>Used only by <c>dotnet ef</c> to create migrations; never at runtime.</summary>
internal sealed class ShippingDbContextFactory : IDesignTimeDbContextFactory<ShippingDbContext>
{
    public ShippingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseNpgsql("Host=localhost;Database=shipping;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options);
}
