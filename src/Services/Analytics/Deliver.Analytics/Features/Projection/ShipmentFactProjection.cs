using Deliver.Analytics.Data;
using Deliver.Contracts.Billing;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;

namespace Deliver.Analytics.Features.Projection;

/// <summary>
/// Projects integration events into <see cref="ShipmentFact"/> rows. One class, many small handlers:
/// each event fills in one fact. The consumer host commits the changes together with the inbox record.
/// </summary>
public sealed class ShipmentFactProjection(AnalyticsDbContext db) :
    IIntegrationEventHandler<ShipmentCreatedIntegrationEvent>,
    IIntegrationEventHandler<ShipmentAssignedIntegrationEvent>,
    IIntegrationEventHandler<ShipmentPickedUpIntegrationEvent>,
    IIntegrationEventHandler<ShipmentInTransitIntegrationEvent>,
    IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>,
    IIntegrationEventHandler<ShipmentCancelledIntegrationEvent>,
    IIntegrationEventHandler<PaymentCapturedIntegrationEvent>,
    IIntegrationEventHandler<ShipmentDeliveryPaymentFailedIntegrationEvent>
{
    public async Task HandleAsync(ShipmentCreatedIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).Created(e.CustomerId, e.CreatedAt);

    public async Task HandleAsync(ShipmentAssignedIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).Assigned(e.AssignedAt);

    public async Task HandleAsync(ShipmentPickedUpIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).PickedUp(e.PickedUpAt);

    public async Task HandleAsync(ShipmentInTransitIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).InTransit(e.InTransitAt);

    public async Task HandleAsync(ShipmentDeliveredIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).Delivered(e.DeliveredAt);

    public async Task HandleAsync(ShipmentCancelledIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).Cancelled(e.CancelledAt);

    public async Task HandleAsync(PaymentCapturedIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).PaymentCaptured(e.Amount, e.Currency);

    public async Task HandleAsync(ShipmentDeliveryPaymentFailedIntegrationEvent e, MessageContext context, CancellationToken ct) =>
        (await db.GetOrCreateFactAsync(e.ShipmentId, ct)).MarkPaymentFailed();
}
