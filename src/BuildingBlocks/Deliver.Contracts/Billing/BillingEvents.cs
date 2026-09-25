using Deliver.Messaging;

namespace Deliver.Contracts.Billing;

public static class BillingExchange
{
    public const string Name = "billing.events";
}

public sealed record DeliveryPriceCalculatedIntegrationEvent(
    Guid ShipmentId,
    Guid InvoiceId,
    decimal Amount,
    string Currency,
    DateTimeOffset CalculatedAt) : IIntegrationEvent
{
    public static string Exchange => BillingExchange.Name;
    public static string RoutingKey => "billing.price_calculated";
}

public sealed record PaymentCapturedIntegrationEvent(
    Guid ShipmentId,
    Guid InvoiceId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset CapturedAt) : IIntegrationEvent
{
    public static string Exchange => BillingExchange.Name;
    public static string RoutingKey => "billing.payment_captured";
}

public sealed record PaymentFailedIntegrationEvent(
    Guid ShipmentId,
    Guid InvoiceId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Reason,
    DateTimeOffset FailedAt) : IIntegrationEvent
{
    public static string Exchange => BillingExchange.Name;
    public static string RoutingKey => "billing.payment_failed";
}
