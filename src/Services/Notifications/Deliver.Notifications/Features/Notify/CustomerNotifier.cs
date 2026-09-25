using Deliver.Messaging;
using Deliver.Notifications.Data;
using Deliver.Notifications.Providers;
using Deliver.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Notifications.Features.Notify;

/// <summary>Sends a message through the provider and records it in the history.</summary>
public sealed class CustomerNotifier(NotificationsDbContext db, INotificationProvider provider, TimeProvider clock)
{
    public async Task NotifyAsync(
        Guid shipmentId,
        Guid customerId,
        string message,
        string triggeredBy,
        MessageContext context,
        CancellationToken cancellationToken,
        NotificationChannel channel = NotificationChannel.Email)
    {
        // A failure here throws, the consumer rolls back and the message is retried / dead-lettered.
        var providerMessageId = await provider.SendAsync(new OutgoingNotification(customerId, channel, message), cancellationToken);

        db.Notifications.Add(new Notification
        {
            ShipmentId = shipmentId,
            CustomerId = customerId,
            Channel = channel,
            Message = message,
            TriggeredBy = triggeredBy,
            SourceMessageId = context.MessageId,
            ProviderMessageId = providerMessageId,
            SentAt = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> GetCustomerOfAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var recipient = await db.ShipmentRecipients.AsNoTracking()
            .FirstOrDefaultAsync(r => r.ShipmentId == shipmentId, cancellationToken);

        // Not projected yet (ShipmentCreated still in flight): fail, and the retry will find it.
        return recipient?.CustomerId ?? throw new NotFoundException("Recipient for shipment", shipmentId);
    }
}
