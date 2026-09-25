using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;
using FluentValidation;
using MediatR;

namespace Deliver.Shipping.Application.Features.ShipmentLifecycle;

// The driver-facing lifecycle steps. Each one is a thin use case: load the aggregate, ask it to change
// state (the aggregate enforces the rules and raises the event), commit.

public sealed record PickUpShipmentCommand(Guid ShipmentId) : IRequest;

public sealed record StartTransitCommand(Guid ShipmentId) : IRequest;

public sealed record DeliverShipmentCommand(Guid ShipmentId) : IRequest;

public sealed record CancelShipmentCommand(Guid ShipmentId, string? Reason) : IRequest;

internal sealed class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
{
    public CancelShipmentCommandValidator() => RuleFor(c => c.Reason).NotEmpty().MaximumLength(500);
}

internal sealed class ShipmentLifecycleHandlers(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock) :
    IRequestHandler<PickUpShipmentCommand>,
    IRequestHandler<StartTransitCommand>,
    IRequestHandler<DeliverShipmentCommand>,
    IRequestHandler<CancelShipmentCommand>
{
    public Task Handle(PickUpShipmentCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.ShipmentId, shipment => shipment.MarkAsPickedUp(clock.GetUtcNow()), cancellationToken);

    public Task Handle(StartTransitCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.ShipmentId, shipment => shipment.MarkAsInTransit(clock.GetUtcNow()), cancellationToken);

    public Task Handle(DeliverShipmentCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.ShipmentId, shipment => shipment.MarkAsDelivered(clock.GetUtcNow()), cancellationToken);

    public Task Handle(CancelShipmentCommand command, CancellationToken cancellationToken) =>
        ChangeAsync(command.ShipmentId, shipment => shipment.Cancel(command.Reason, clock.GetUtcNow()), cancellationToken);

    private async Task ChangeAsync(Guid shipmentId, Action<Shipment> change, CancellationToken cancellationToken)
    {
        var shipment = await shipments.GetAsync(new ShipmentId(shipmentId), cancellationToken)
            ?? throw new NotFoundException("Shipment", shipmentId);

        change(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
