using Deliver.SharedKernel;

namespace Deliver.Billing.Domain.Invoices;

public sealed record Money
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new InvalidValueException("Amount cannot be negative.");

        Amount = amount;
        Currency = Ensure.NotEmpty(currency, "Currency", maxLength: 3).ToUpperInvariant();
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
