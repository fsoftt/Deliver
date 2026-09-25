using Deliver.Billing.Application.Features.Payments;
using Deliver.Billing.Domain.Invoices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Deliver.Billing.Infrastructure.Payments;

public sealed class FakePaymentGatewayOptions
{
    public const string SectionName = "Billing:FakePaymentGateway";

    /// <summary>Charges above this amount are declined ("card limit exceeded"). Handy to demo the saga's failure path.</summary>
    public decimal MaxAmount { get; set; } = 100_000;

    public List<Guid> DeclinedCustomerIds { get; set; } = [];
}

/// <summary>Stand-in for a real payment provider (non-goal). Deterministic so demos and tests are repeatable.</summary>
internal sealed class FakePaymentGateway(IOptions<FakePaymentGatewayOptions> options, ILogger<FakePaymentGateway> logger)
    : IPaymentGateway
{
    public Task<PaymentResult> ChargeAsync(ChargeRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var result = request.Amount.Amount > settings.MaxAmount
            ? PaymentResult.Declined($"Amount exceeds card limit of {settings.MaxAmount} {request.Amount.Currency}.")
            : settings.DeclinedCustomerIds.Contains(request.CustomerId)
                ? PaymentResult.Declined("Card declined by issuer.")
                : PaymentResult.Success($"fake_{request.IdempotencyKey:N}");

        logger.LogInformation("Fake charge of {Amount} {Currency} for customer {CustomerId}: {Outcome}",
            request.Amount.Amount, request.Amount.Currency, request.CustomerId, result.Succeeded ? "captured" : result.FailureReason);

        return Task.FromResult(result);
    }
}
