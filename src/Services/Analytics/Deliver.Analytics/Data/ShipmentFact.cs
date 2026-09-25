namespace Deliver.Analytics.Data;

/// <summary>
/// One row per shipment, denormalised for reporting and built only from events.
/// Designed to tolerate out-of-order and duplicated events: every event only fills in its own
/// timestamp, and the status is derived from the facts, so the arrival order does not matter.
/// Durations are computed when the facts arrive, so reports are simple aggregates.
/// </summary>
public sealed class ShipmentFact
{
    private ShipmentFact()
    {
    }

    public Guid ShipmentId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string Status { get; private set; } = "Unknown";
    public DateTimeOffset? CreatedAt { get; private set; }
    public DateOnly? CreatedOn { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? InTransitAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public double? MinutesToAssign { get; private set; }
    public double? MinutesToDeliver { get; private set; }
    public decimal? Revenue { get; private set; }
    public string? Currency { get; private set; }
    public bool PaymentFailed { get; private set; }

    public static ShipmentFact For(Guid shipmentId) => new() { ShipmentId = shipmentId };

    public void Created(Guid customerId, DateTimeOffset at)
    {
        CustomerId = customerId;
        CreatedAt = at;
        CreatedOn = DateOnly.FromDateTime(at.UtcDateTime);
        Recalculate();
    }

    public void Assigned(DateTimeOffset at)
    {
        AssignedAt = at;
        Recalculate();
    }

    public void PickedUp(DateTimeOffset at)
    {
        PickedUpAt = at;
        Recalculate();
    }

    public void InTransit(DateTimeOffset at)
    {
        InTransitAt = at;
        Recalculate();
    }

    public void Delivered(DateTimeOffset at)
    {
        DeliveredAt = at;
        Recalculate();
    }

    public void Cancelled(DateTimeOffset at)
    {
        CancelledAt = at;
        Recalculate();
    }

    public void PaymentCaptured(decimal amount, string currency)
    {
        Revenue = amount;
        Currency = currency;
        PaymentFailed = false;
    }

    public void MarkPaymentFailed() => PaymentFailed = Revenue is null;

    private void Recalculate()
    {
        Status = CancelledAt is not null ? "Cancelled"
            : DeliveredAt is not null ? "Delivered"
            : InTransitAt is not null ? "InTransit"
            : PickedUpAt is not null ? "PickedUp"
            : AssignedAt is not null ? "Assigned"
            : CreatedAt is not null ? "Created"
            : "Unknown";

        MinutesToAssign = CreatedAt is { } c1 && AssignedAt is { } a ? (a - c1).TotalMinutes : null;
        MinutesToDeliver = CreatedAt is { } c2 && DeliveredAt is { } d ? (d - c2).TotalMinutes : null;
    }
}
