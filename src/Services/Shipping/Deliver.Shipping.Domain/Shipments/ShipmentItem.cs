using Deliver.SharedKernel;

namespace Deliver.Shipping.Domain.Shipments;

/// <summary>A line of goods inside a shipment. Value object: it has no identity of its own.</summary>
public sealed class ShipmentItem
{
    private ShipmentItem()
    {
    }

    public string Description { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Weight UnitWeight { get; private set; }

    public Weight TotalWeight => UnitWeight * Quantity;

    public static ShipmentItem Create(string? description, int quantity, decimal unitWeightKg) => new()
    {
        Description = Ensure.NotEmpty(description, "Item description"),
        Quantity = Ensure.Positive(quantity, "Item quantity"),
        UnitWeight = new Weight(unitWeightKg),
    };
}
