using Deliver.Messaging;

namespace Deliver.Contracts.Shipping;

public static class ShippingExchange
{
    public const string Name = "shipment.events";
}

public sealed record AddressDto(string Street, string City, string PostalCode);

public sealed record ShipmentCreatedIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    AddressDto PickupAddress,
    AddressDto DeliveryAddress,
    decimal TotalWeightKg,
    int ItemCount,
    DateTimeOffset CreatedAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.created";
}

public sealed record ShipmentAssignedIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    Guid DriverId,
    DateTimeOffset AssignedAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.assigned";
}

public sealed record ShipmentPickedUpIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    Guid DriverId,
    DateTimeOffset PickedUpAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.picked_up";
}

public sealed record ShipmentInTransitIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    Guid DriverId,
    DateTimeOffset InTransitAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.in_transit";
}

public sealed record ShipmentDeliveredIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    Guid DriverId,
    DateTimeOffset DeliveredAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.delivered";
}

public sealed record ShipmentCancelledIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    Guid? DriverId,
    string Reason,
    DateTimeOffset CancelledAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.cancelled";
}

/// <summary>The shipment was delivered but the payment for it failed (end state of the delivery saga).</summary>
public sealed record ShipmentDeliveryPaymentFailedIntegrationEvent(
    Guid ShipmentId,
    Guid CustomerId,
    string Reason,
    DateTimeOffset FailedAt) : IIntegrationEvent
{
    public static string Exchange => ShippingExchange.Name;
    public static string RoutingKey => "shipment.delivery_payment_failed";
}
