using Deliver.Billing.Domain.Invoices;

namespace Deliver.Billing.Application.Features.Payments;

/// <summary>
/// Port to the payment provider. <paramref name="IdempotencyKey"/> is the invoice id: real providers
/// (Stripe, Adyen, ...) use it to make a repeated charge request a no-op, which protects the customer
/// even if our process crashes between charging and committing.
/// </summary>
public sealed record ChargeRequest(Guid IdempotencyKey, Guid CustomerId, Money Amount);

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(ChargeRequest request, CancellationToken cancellationToken);
}
