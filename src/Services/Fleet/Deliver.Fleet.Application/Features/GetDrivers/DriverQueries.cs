using MediatR;

namespace Deliver.Fleet.Application.Features.GetDrivers;

public sealed record GetDriverQuery(Guid DriverId) : IRequest<DriverDetails?>;

/// <summary>Everyone (for the operations view), or only drivers currently able to take work.</summary>
public sealed record ListDriversQuery(bool OnlyAvailable) : IRequest<IReadOnlyList<DriverDetails>>;

internal sealed class DriverQueryHandlers(IDriverReadStore store) :
    IRequestHandler<GetDriverQuery, DriverDetails?>,
    IRequestHandler<ListDriversQuery, IReadOnlyList<DriverDetails>>
{
    public Task<DriverDetails?> Handle(GetDriverQuery query, CancellationToken cancellationToken) =>
        store.GetAsync(query.DriverId, cancellationToken);

    public Task<IReadOnlyList<DriverDetails>> Handle(ListDriversQuery query, CancellationToken cancellationToken) =>
        store.ListAsync(query.OnlyAvailable, cancellationToken);
}
