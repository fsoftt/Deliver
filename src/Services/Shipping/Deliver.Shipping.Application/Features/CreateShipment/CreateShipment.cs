using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;
using FluentValidation;
using MediatR;

namespace Deliver.Shipping.Application.Features.CreateShipment;

public sealed record CreateShipmentCommand(
    Guid CustomerId,
    AddressInput PickupAddress,
    AddressInput DeliveryAddress,
    IReadOnlyList<ItemInput> Items) : IRequest<ShipmentId>;

public sealed record AddressInput(string Street, string City, string PostalCode);

public sealed record ItemInput(string Description, int Quantity, decimal UnitWeightKg);

/// <summary>Input shape only. Rules such as "pickup and delivery must differ" live in the aggregate.</summary>
internal sealed class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.PickupAddress).NotNull().SetValidator(new AddressInputValidator());
        RuleFor(c => c.DeliveryAddress).NotNull().SetValidator(new AddressInputValidator());
        RuleFor(c => c.Items).NotEmpty().WithMessage("A shipment must contain at least one item.");
        RuleFor(c => c.Items.Count).LessThanOrEqualTo(50).When(c => c.Items is not null);
        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitWeightKg).GreaterThan(0).LessThanOrEqualTo(1000);
        });
    }

    private sealed class AddressInputValidator : AbstractValidator<AddressInput>
    {
        public AddressInputValidator()
        {
            RuleFor(a => a.Street).NotEmpty().MaximumLength(200);
            RuleFor(a => a.City).NotEmpty().MaximumLength(100);
            RuleFor(a => a.PostalCode).NotEmpty().MaximumLength(20);
        }
    }
}

/// <summary>
/// Creates the shipment and returns immediately. Pricing, dispatch, notifications and analytics
/// all react to <c>ShipmentCreated</c> asynchronously; none of them can make this request fail.
/// </summary>
internal sealed class CreateShipmentHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<CreateShipmentCommand, ShipmentId>
{
    public async Task<ShipmentId> Handle(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        var items = command.Items
            .Select(item => ShipmentItem.Create(item.Description, item.Quantity, item.UnitWeightKg))
            .ToList();

        var shipment = Shipment.Create(
            CustomerId.From(command.CustomerId),
            Address.Create(command.PickupAddress.Street, command.PickupAddress.City, command.PickupAddress.PostalCode),
            Address.Create(command.DeliveryAddress.Street, command.DeliveryAddress.City, command.DeliveryAddress.PostalCode),
            items,
            clock.GetUtcNow());

        shipments.Add(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.Id;
    }
}
