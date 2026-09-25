using Deliver.Messaging.Inbox;
using Deliver.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Messaging;

/// <summary>Maps the outbox / inbox tables into a service's own DbContext (and therefore its own database).</summary>
public static class MessagingModelBuilderExtensions
{
    public static ModelBuilder AddOutboxMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(200);
            b.Property(x => x.Exchange).HasColumnName("exchange").HasMaxLength(200);
            b.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(200);
            b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            b.Property(x => x.TraceParent).HasColumnName("trace_parent").HasMaxLength(100);
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            b.Property(x => x.Attempts).HasColumnName("attempts");
            b.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);

            // Partial index: the processor only ever scans unpublished rows.
            b.HasIndex(x => x.OccurredAt)
                .HasDatabaseName("ix_outbox_messages_unprocessed")
                .HasFilter("processed_at IS NULL");
        });
        return modelBuilder;
    }

    public static ModelBuilder AddInboxMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedMessage>(b =>
        {
            b.ToTable("processed_messages");
            b.HasKey(x => new { x.MessageId, x.Consumer });
            b.Property(x => x.MessageId).HasColumnName("message_id");
            b.Property(x => x.Consumer).HasColumnName("consumer").HasMaxLength(200);
            b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        });
        return modelBuilder;
    }
}
