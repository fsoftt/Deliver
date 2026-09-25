namespace Deliver.Shipping.Domain.Shipments;

/// <summary>Collection-like access to Shipment aggregates. Reads for display go through a separate read store.</summary>
public interface IShipmentRepository
{
    Task<Shipment?> GetAsync(ShipmentId id, CancellationToken cancellationToken);

    void Add(Shipment shipment);
}
