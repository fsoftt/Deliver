using Deliver.Billing.Application.Features.GetInvoice;
using Deliver.Billing.Domain.Invoices;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deliver.Billing.Infrastructure.Persistence;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(invoice =>
        {
            invoice.ToTable("invoices");
            invoice.HasKey(i => i.Id);
            invoice.Property(i => i.Id).ValueGeneratedNever();
            invoice.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            invoice.Property(i => i.Version).IsRowVersion();
            invoice.HasIndex(i => i.ShipmentId).IsUnique();

            invoice.OwnsOne(i => i.Price, price =>
            {
                price.Property(m => m.Amount).HasColumnName("price_amount").HasPrecision(12, 2);
                price.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3);
            });
            invoice.Navigation(i => i.Price).IsRequired();

            invoice.OwnsMany(i => i.Payments, payment =>
            {
                payment.ToTable("payment_attempts");
                payment.WithOwner().HasForeignKey("invoice_id");
                payment.HasKey(p => p.Id);
                payment.Property(p => p.Id).ValueGeneratedNever();
                payment.Property(p => p.Amount).HasPrecision(12, 2);
                payment.Property(p => p.ProviderReference).HasMaxLength(100);
                payment.Property(p => p.FailureReason).HasMaxLength(500);
            });
            invoice.Navigation(i => i.Payments).HasField("_payments").UsePropertyAccessMode(PropertyAccessMode.Field);
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
            throw new DomainException("The invoice was modified by another operation. Please retry.");
        }
    }
}

internal sealed class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql("Host=localhost;Database=billing;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options);
}

internal sealed class InvoiceRepository(BillingDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        db.Invoices.FirstOrDefaultAsync(i => i.ShipmentId == shipmentId, cancellationToken);

    public void Add(Invoice invoice) => db.Invoices.Add(invoice);
}

internal sealed class InvoiceReadStore(BillingDbContext db) : IInvoiceReadStore
{
    public async Task<InvoiceDetails?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.ShipmentId == shipmentId, cancellationToken);
        return invoice is null
            ? null
            : new InvoiceDetails(
                invoice.Id,
                invoice.ShipmentId,
                invoice.CustomerId,
                invoice.Price.Amount,
                invoice.Price.Currency,
                invoice.Status.ToString(),
                invoice.IssuedAt,
                invoice.PaidAt,
                invoice.VoidedAt,
                invoice.Payments
                    .OrderBy(p => p.AttemptedAt)
                    .Select(p => new PaymentAttemptView(p.Id, p.Amount, p.Succeeded, p.ProviderReference, p.FailureReason, p.AttemptedAt))
                    .ToList());
    }
}
