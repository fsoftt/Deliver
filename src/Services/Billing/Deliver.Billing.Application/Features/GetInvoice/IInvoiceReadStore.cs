namespace Deliver.Billing.Application.Features.GetInvoice;

public interface IInvoiceReadStore
{
    Task<InvoiceDetails?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);
}

public sealed record BillingSummary(Guid ShipmentId, Guid InvoiceId, decimal Amount, string Currency, string Status);

public sealed record InvoiceDetails(
    Guid InvoiceId,
    Guid ShipmentId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? VoidedAt,
    IReadOnlyList<PaymentAttemptView> Payments)
{
    public BillingSummary ToSummary() => new(ShipmentId, InvoiceId, Amount, Currency, Status);
}

public sealed record PaymentAttemptView(Guid Id, decimal Amount, bool Succeeded, string? ProviderReference, string? FailureReason, DateTimeOffset AttemptedAt);
