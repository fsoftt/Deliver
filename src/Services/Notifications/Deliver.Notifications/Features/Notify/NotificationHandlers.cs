using Deliver.Contracts.Dispatch;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.Notifications.Data;
using Microsoft.EntityFrameworkCore;

namespace Deliver.Notifications.Features.Notify;

// One handler per event the customer cares about. No shipment rules live here: the Notification context
// only decides WHAT to say and HOW to say it.

public sealed class ShipmentCreatedHandler(NotificationsDbContext db, CustomerNotifier notifier)
    : IIntegrationEventHandler<ShipmentCreatedIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCreatedIntegrationEvent e, MessageContext context, CancellationToken cancellationToken)
    {
        if (!await db.ShipmentRecipients.AnyAsync(r => r.ShipmentId == e.ShipmentId, cancellationToken))
            db.ShipmentRecipients.Add(new ShipmentRecipient { ShipmentId = e.ShipmentId, CustomerId = e.CustomerId });

        await notifier.NotifyAsync(e.ShipmentId, e.CustomerId,
            $"We received your shipment to {e.DeliveryAddress.City}. We are looking for a driver.",
            "ShipmentCreated", context, cancellationToken);
    }
}

public sealed class DriverAssignedHandler(CustomerNotifier notifier) : IIntegrationEventHandler<DriverAssignedIntegrationEvent>
{
    public async Task HandleAsync(DriverAssignedIntegrationEvent e, MessageContext context, CancellationToken cancellationToken)
    {
        var customerId = await notifier.GetCustomerOfAsync(e.ShipmentId, cancellationToken);
        await notifier.NotifyAsync(e.ShipmentId, customerId,
            $"Your shipment has been assigned to driver {e.DriverName}.",
            "DriverAssigned", context, cancellationToken, NotificationChannel.Push);
    }
}

public sealed class ShipmentPickedUpHandler(CustomerNotifier notifier) : IIntegrationEventHandler<ShipmentPickedUpIntegrationEvent>
{
    public Task HandleAsync(ShipmentPickedUpIntegrationEvent e, MessageContext context, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(e.ShipmentId, e.CustomerId, "Your shipment has been picked up.",
            "ShipmentPickedUp", context, cancellationToken, NotificationChannel.Push);
}

public sealed class ShipmentInTransitHandler(CustomerNotifier notifier) : IIntegrationEventHandler<ShipmentInTransitIntegrationEvent>
{
    public Task HandleAsync(ShipmentInTransitIntegrationEvent e, MessageContext context, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(e.ShipmentId, e.CustomerId, "Your shipment is on its way.",
            "ShipmentInTransit", context, cancellationToken, NotificationChannel.Sms);
}

public sealed class ShipmentDeliveredHandler(CustomerNotifier notifier) : IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>
{
    public Task HandleAsync(ShipmentDeliveredIntegrationEvent e, MessageContext context, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(e.ShipmentId, e.CustomerId, "Your shipment has been delivered. Thank you!",
            "ShipmentDelivered", context, cancellationToken);
}

public sealed class ShipmentCancelledHandler(CustomerNotifier notifier) : IIntegrationEventHandler<ShipmentCancelledIntegrationEvent>
{
    public Task HandleAsync(ShipmentCancelledIntegrationEvent e, MessageContext context, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(e.ShipmentId, e.CustomerId, $"Your shipment was cancelled: {e.Reason}",
            "ShipmentCancelled", context, cancellationToken);
}

public sealed class ShipmentDeliveryPaymentFailedHandler(CustomerNotifier notifier)
    : IIntegrationEventHandler<ShipmentDeliveryPaymentFailedIntegrationEvent>
{
    public Task HandleAsync(ShipmentDeliveryPaymentFailedIntegrationEvent e, MessageContext context, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(e.ShipmentId, e.CustomerId, $"We could not charge your delivery: {e.Reason} Please update your payment method.",
            "ShipmentDeliveryPaymentFailed", context, cancellationToken);
}
