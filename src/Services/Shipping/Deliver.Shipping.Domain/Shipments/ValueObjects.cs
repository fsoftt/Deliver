using Deliver.SharedKernel;

namespace Deliver.Shipping.Domain.Shipments;

public readonly record struct ShipmentId(Guid Value)
{
    public static ShipmentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId From(Guid value) => new(Ensure.NotEmpty(value, "Customer id"));

    public override string ToString() => Value.ToString();
}

/// <summary>Shipping only needs to know WHICH driver carries a shipment; drivers themselves belong to Fleet.</summary>
public readonly record struct DriverId(Guid Value)
{
    public static DriverId From(Guid value) => new(Ensure.NotEmpty(value, "Driver id"));

    public override string ToString() => Value.ToString();
}

public sealed record Address
{
    private Address()
    {
    }

    public string Street { get; private init; } = null!;
    public string City { get; private init; } = null!;
    public string PostalCode { get; private init; } = null!;

    public static Address Create(string? street, string? city, string? postalCode) => new()
    {
        Street = Ensure.NotEmpty(street, "Street"),
        City = Ensure.NotEmpty(city, "City", maxLength: 100),
        PostalCode = Ensure.NotEmpty(postalCode, "Postal code", maxLength: 20),
    };
}

public readonly record struct Weight
{
    public Weight(decimal kilograms)
    {
        Kilograms = Ensure.Positive(kilograms, "Weight");
    }

    public decimal Kilograms { get; }

    public static Weight operator +(Weight left, Weight right) => new(left.Kilograms + right.Kilograms);

    public static Weight operator *(Weight weight, int quantity) => new(weight.Kilograms * quantity);
}

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
