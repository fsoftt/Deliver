using Deliver.Billing.Domain.Invoices;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;
using Deliver.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Deliver.Billing.Application.Features.Payments;

/// <summary>
/// Charges the customer once the shipment is delivered. Protection against double charging is layered:
/// the inbox (same message id) → the aggregate (already paid) → the provider idempotency key.
/// </summary>
public sealed class ShipmentDeliveredHandler(
    IInvoiceRepository invoices,
    IPaymentGateway paymentGateway,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<ShipmentDeliveredHandler> logger) : IIntegrationEventHandler<ShipmentDeliveredIntegrationEvent>
{
    public async Task HandleAsync(ShipmentDeliveredIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        // Missing invoice = ShipmentCreated not processed yet; throwing lets the retry resolve the ordering.
        var invoice = await invoices.GetByShipmentAsync(integrationEvent.ShipmentId, cancellationToken)
            ?? throw new NotFoundException("Invoice for shipment", integrationEvent.ShipmentId);

        if (invoice.Status != InvoiceStatus.Open)
        {
            logger.LogInformation("Invoice {InvoiceId} is already {Status}; not charging again", invoice.Id, invoice.Status);
            return;
        }

        invoice.EnsureCanBeCharged();
        var result = await paymentGateway.ChargeAsync(new ChargeRequest(invoice.Id, invoice.CustomerId, invoice.Price), cancellationToken);
        invoice.RecordPayment(result, clock.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
