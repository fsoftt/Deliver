using MediatR;

namespace Deliver.Dispatch.Application.Features.GetAssignments;

public sealed record GetAssignmentByShipmentQuery(Guid ShipmentId) : IRequest<AssignmentView?>;

public sealed record ListAssignmentsQuery(string? Status) : IRequest<IReadOnlyList<AssignmentView>>;

internal sealed class AssignmentQueryHandlers(IAssignmentReadStore store) :
    IRequestHandler<GetAssignmentByShipmentQuery, AssignmentView?>,
    IRequestHandler<ListAssignmentsQuery, IReadOnlyList<AssignmentView>>
{
    public Task<AssignmentView?> Handle(GetAssignmentByShipmentQuery query, CancellationToken cancellationToken) =>
        store.GetByShipmentAsync(query.ShipmentId, cancellationToken);

    public Task<IReadOnlyList<AssignmentView>> Handle(ListAssignmentsQuery query, CancellationToken cancellationToken) =>
        store.ListAsync(query.Status, cancellationToken);
}
