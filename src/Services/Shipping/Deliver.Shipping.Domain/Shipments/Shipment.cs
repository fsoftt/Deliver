using Deliver.SharedKernel;

namespace Deliver.Shipping.Domain.Shipments;

/// <summary>
/// Aggregate root of the Shipping context. The only way to change a shipment is through its
/// behaviour methods, which guard the lifecycle:
/// <code>
/// Created → Assigned → PickedUp → InTransit → Delivered
///    └──────────┴──→ Cancelled   (only before pickup)
/// </code>
/// </summary>
public sealed class Shipment : AggregateRoot<ShipmentId>
{
    private readonly List<ShipmentItem> _items = [];
    private decimal? _priceAmount;
    private string? _priceCurrency;

    private Shipment()
    {
    }

    public CustomerId CustomerId { get; private set; }
    public Address PickupAddress { get; private set; } = null!;
    public Address DeliveryAddress { get; private set; } = null!;
    public IReadOnlyCollection<ShipmentItem> Items => _items.AsReadOnly();
    public ShipmentStatus Status { get; private set; }
    public DriverId? DriverId { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }

    /// <summary>Quoted asynchronously by Billing: <c>null</c> until the price is known (eventual consistency).</summary>
    public Money? Price => _priceAmount is null ? null : new Money(_priceAmount.Value, _priceCurrency!);

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? InTransitAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    /// <summary>Optimistic concurrency token (PostgreSQL <c>xmin</c>).</summary>
    public uint Version { get; private set; }

    public Weight TotalWeight => _items.Select(item => item.TotalWeight).Aggregate((a, b) => a + b);

    public static Shipment Create(
        CustomerId customerId,
        Address pickupAddress,
        Address deliveryAddress,
        IReadOnlyCollection<ShipmentItem> items,
        DateTimeOffset now)
    {
        if (items.Count == 0)
            throw new InvalidValueException("A shipment must contain at least one item.");

        if (pickupAddress == deliveryAddress)
            throw new InvalidValueException("Pickup and delivery addresses must be different.");

        var shipment = new Shipment
        {
            Id = ShipmentId.New(),
            CustomerId = customerId,
            PickupAddress = pickupAddress,
            DeliveryAddress = deliveryAddress,
            Status = ShipmentStatus.Created,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = now,
        };
        shipment._items.AddRange(items);

        shipment.Raise(new ShipmentCreatedDomainEvent(
            shipment.Id, customerId, pickupAddress, deliveryAddress, shipment.TotalWeight, items.Count, now));

        return shipment;
    }

    /// <summary>
    /// Assigns (or re-assigns, before pickup) a driver. Re-assignment happens when Fleet rejects
    /// the first driver and Dispatch picks another one.
    /// </summary>
    public void AssignDriver(DriverId driverId, DateTimeOffset now)
    {
        if (Status == ShipmentStatus.Assigned && DriverId == driverId)
            return; // Same assignment received again: nothing changes.

        EnsureStatus("assign a driver to", ShipmentStatus.Created, ShipmentStatus.Assigned);

        DriverId = driverId;
        Status = ShipmentStatus.Assigned;
        AssignedAt = now;
        Raise(new DriverAssignedDomainEvent(Id, CustomerId, driverId, now));
    }

    public void MarkAsPickedUp(DateTimeOffset now)
    {
        EnsureStatus("pick up", ShipmentStatus.Assigned);

        Status = ShipmentStatus.PickedUp;
        PickedUpAt = now;
        Raise(new ShipmentPickedUpDomainEvent(Id, CustomerId, DriverId!.Value, now));
    }

    public void MarkAsInTransit(DateTimeOffset now)
    {
        EnsureStatus("move to transit", ShipmentStatus.PickedUp);

        Status = ShipmentStatus.InTransit;
        InTransitAt = now;
        Raise(new ShipmentInTransitDomainEvent(Id, CustomerId, DriverId!.Value, now));
    }

    public void MarkAsDelivered(DateTimeOffset now)
    {
        EnsureStatus("deliver", ShipmentStatus.InTransit);

        Status = ShipmentStatus.Delivered;
        DeliveredAt = now;
        Raise(new ShipmentDeliveredDomainEvent(Id, CustomerId, DriverId!.Value, now));
    }

    /// <summary>Goods that are already with the driver cannot be cancelled (that would be a return, out of scope).</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        EnsureStatus("cancel", ShipmentStatus.Created, ShipmentStatus.Assigned);

        Status = ShipmentStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = Ensure.NotEmpty(reason, "Cancellation reason", maxLength: 500);
        Raise(new ShipmentCancelledDomainEvent(Id, CustomerId, DriverId, CancellationReason, now));
    }

    public void QuotePrice(Money price)
    {
        _priceAmount = price.Amount;
        _priceCurrency = price.Currency;
    }

    public void RecordPaymentCaptured()
    {
        EnsureStatus("record a payment for", ShipmentStatus.Delivered);
        PaymentStatus = PaymentStatus.Captured;
    }

    /// <summary>Compensation end-state of the delivery saga: delivered, but not paid.</summary>
    public void RecordPaymentFailed(string reason, DateTimeOffset now)
    {
        EnsureStatus("record a payment for", ShipmentStatus.Delivered);
        if (PaymentStatus == PaymentStatus.Failed)
            return;

        PaymentStatus = PaymentStatus.Failed;
        Raise(new ShipmentDeliveryPaymentFailedDomainEvent(Id, CustomerId, reason, now));
    }

    private void EnsureStatus(string action, params ShipmentStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException($"Cannot {action} a shipment that is {Status}.");
    }
}
