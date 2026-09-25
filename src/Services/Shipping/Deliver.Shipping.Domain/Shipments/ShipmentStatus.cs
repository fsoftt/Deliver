namespace Deliver.Shipping.Domain.Shipments;

public enum ShipmentStatus
{
    Created,
    Assigned,
    PickedUp,
    InTransit,
    Delivered,
    Cancelled,
}

/// <summary>Settlement of a delivered shipment, reported asynchronously by Billing.</summary>
public enum PaymentStatus
{
    Pending,
    Captured,
    Failed,
}
