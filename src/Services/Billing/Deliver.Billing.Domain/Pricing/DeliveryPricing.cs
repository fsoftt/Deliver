using Deliver.Billing.Domain.Invoices;
using Deliver.SharedKernel;

namespace Deliver.Billing.Domain.Pricing;

/// <summary>A price list. Amounts are in whole units of the currency (CLP has no cents).</summary>
public sealed record Tariff(decimal BaseFee, decimal PerKilogram, decimal PerExtraItem, string Currency)
{
    public static readonly Tariff Standard = new(BaseFee: 4000, PerKilogram: 1500, PerExtraItem: 500, Currency: "CLP");
}

/// <summary>
/// Domain service that turns what is being shipped into a delivery price:
/// base fee + every started kilogram + every item beyond the first.
/// Deliberately simple (complex pricing is a non-goal), but it lives in the domain and is unit-tested.
/// </summary>
public static class DeliveryPricing
{
    public static Money Calculate(decimal totalWeightKg, int itemCount, Tariff tariff)
    {
        Ensure.Positive(totalWeightKg, "Total weight");
        Ensure.Positive(itemCount, "Item count");

        var chargeableKilograms = Math.Ceiling(totalWeightKg);
        var amount = tariff.BaseFee
                     + chargeableKilograms * tariff.PerKilogram
                     + (itemCount - 1) * tariff.PerExtraItem;

        return new Money(amount, tariff.Currency);
    }
}
