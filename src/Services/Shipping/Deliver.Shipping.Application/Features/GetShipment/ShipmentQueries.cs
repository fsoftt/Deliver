using MediatR;

namespace Deliver.Shipping.Application.Features.GetShipment;

public sealed record GetShipmentQuery(Guid ShipmentId) : IRequest<ShipmentDetails?>;

public sealed record ListShipmentsQuery(string? Status, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ShipmentSummary>>;

internal sealed class ShipmentQueryHandlers(IShipmentReadStore store) :
    IRequestHandler<GetShipmentQuery, ShipmentDetails?>,
    IRequestHandler<ListShipmentsQuery, PagedResult<ShipmentSummary>>
{
    public Task<ShipmentDetails?> Handle(GetShipmentQuery query, CancellationToken cancellationToken) =>
        store.GetAsync(query.ShipmentId, cancellationToken);

    public Task<PagedResult<ShipmentSummary>> Handle(ListShipmentsQuery query, CancellationToken cancellationToken) =>
        store.ListAsync(query.Status, query.Page, query.PageSize, cancellationToken);
}
