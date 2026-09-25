using MediatR;

namespace Deliver.Billing.Application.Features.GetInvoice;

public sealed record GetInvoiceQuery(Guid ShipmentId) : IRequest<InvoiceDetails?>;

internal sealed class GetInvoiceHandler(IInvoiceReadStore store) : IRequestHandler<GetInvoiceQuery, InvoiceDetails?>
{
    public Task<InvoiceDetails?> Handle(GetInvoiceQuery query, CancellationToken cancellationToken) =>
        store.GetByShipmentAsync(query.ShipmentId, cancellationToken);
}
