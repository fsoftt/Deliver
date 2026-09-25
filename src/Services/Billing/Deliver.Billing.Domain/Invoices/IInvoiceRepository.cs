namespace Deliver.Billing.Domain.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByShipmentAsync(Guid shipmentId, CancellationToken cancellationToken);

    void Add(Invoice invoice);
}
