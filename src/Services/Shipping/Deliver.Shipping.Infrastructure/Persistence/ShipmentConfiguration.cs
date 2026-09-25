using Deliver.Shipping.Domain.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Deliver.Shipping.Infrastructure.Persistence;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new ShipmentId(value))
            .ValueGeneratedNever();
        builder.Property(s => s.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value));
        builder.Property(s => s.DriverId)
            .HasConversion(new ValueConverter<DriverId, Guid>(id => id.Value, value => new DriverId(value)));

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);

        builder.OwnsOne(s => s.PickupAddress, ConfigureAddress);
        builder.OwnsOne(s => s.DeliveryAddress, ConfigureAddress);

        builder.OwnsMany(s => s.Items, items =>
        {
            items.ToTable("shipment_items");
            items.WithOwner().HasForeignKey("shipment_id");
            items.Property<int>("id");
            items.HasKey("id");
            items.Property(i => i.Description).HasMaxLength(200);
            items.Property(i => i.UnitWeight)
                .HasConversion(weight => weight.Kilograms, kilograms => new Weight(kilograms))
                .HasColumnName("unit_weight_kg")
                .HasPrecision(10, 3);
        });
        builder.Navigation(s => s.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);

        // Money is nullable until Billing quotes it, so it is mapped through two private fields.
        builder.Property<decimal?>("_priceAmount").HasColumnName("price_amount").HasPrecision(12, 2);
        builder.Property<string?>("_priceCurrency").HasColumnName("price_currency").HasMaxLength(3);

        // PostgreSQL's xmin system column as optimistic concurrency token: two concurrent updates of the
        // same shipment (e.g. an HTTP request and a consumer) cannot silently overwrite each other.
        builder.Property(s => s.Version).IsRowVersion();

        builder.HasIndex(s => new { s.Status, s.CreatedAt });
    }

    private static void ConfigureAddress<TOwner>(OwnedNavigationBuilder<TOwner, Address> address)
        where TOwner : class
    {
        address.Property(a => a.Street).HasMaxLength(200);
        address.Property(a => a.City).HasMaxLength(100);
        address.Property(a => a.PostalCode).HasMaxLength(20);
    }
}
