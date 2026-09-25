using Deliver.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deliver.Notifications.Data;

public enum NotificationChannel
{
    Email,
    Sms,
    Push,
}

/// <summary>History of every notification that was sent.</summary>
public sealed class Notification
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid ShipmentId { get; init; }
    public required Guid CustomerId { get; init; }
    public required NotificationChannel Channel { get; init; }
    public required string Message { get; init; }
    public required string TriggeredBy { get; init; }
    public required Guid SourceMessageId { get; init; }
    public required string ProviderMessageId { get; init; }
    public required DateTimeOffset SentAt { get; init; }
}

/// <summary>
/// Local projection: who is the customer of each shipment? Built from ShipmentCreated so that events
/// that do not carry the customer (e.g. Dispatch's DriverAssigned) can still be routed to them,
/// without calling the Shipping service.
/// </summary>
public sealed class ShipmentRecipient
{
    public required Guid ShipmentId { get; init; }
    public required Guid CustomerId { get; init; }
}

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ShipmentRecipient> ShipmentRecipients => Set<ShipmentRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(notification =>
        {
            notification.ToTable("notifications");
            notification.Property(n => n.Channel).HasConversion<string>().HasMaxLength(10);
            notification.Property(n => n.Message).HasMaxLength(500);
            notification.Property(n => n.TriggeredBy).HasMaxLength(100);
            notification.Property(n => n.ProviderMessageId).HasMaxLength(100);
            notification.HasIndex(n => n.ShipmentId);
            notification.HasIndex(n => n.CustomerId);
        });

        modelBuilder.Entity<ShipmentRecipient>(recipient =>
        {
            recipient.ToTable("shipment_recipients");
            recipient.HasKey(r => r.ShipmentId);
        });

        modelBuilder.AddInboxMessages();
    }
}

internal sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql("Host=localhost;Database=notifications;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options);
}
