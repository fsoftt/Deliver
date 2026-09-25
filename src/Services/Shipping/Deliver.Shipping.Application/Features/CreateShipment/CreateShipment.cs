using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;

namespace Deliver.Shipping.Application.Features.CreateShipment;

public sealed record CreateShipmentCommand(
    Guid CustomerId,
    AddressInput PickupAddress,
    AddressInput DeliveryAddress,
    IReadOnlyList<ItemInput> Items);

public sealed record AddressInput(string Street, string City, string PostalCode);

public sealed record ItemInput(string Description, int Quantity, decimal UnitWeightKg);

/// <summary>
/// Creates the shipment and returns immediately. Pricing, dispatch, notifications and analytics
/// all react to <c>ShipmentCreated</c> asynchronously; none of them can make this request fail.
/// </summary>
public sealed class CreateShipmentHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<ShipmentId> HandleAsync(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        var items = (command.Items ?? [])
            .Select(item => ShipmentItem.Create(item.Description, item.Quantity, item.UnitWeightKg))
            .ToList();

        var shipment = Shipment.Create(
            CustomerId.From(command.CustomerId),
            ToAddress(command.PickupAddress),
            ToAddress(command.DeliveryAddress),
            items,
            clock.GetUtcNow());

        shipments.Add(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.Id;
    }

    private static Address ToAddress(AddressInput? input) =>
        input is null
            ? throw new InvalidValueException("Address is required.")
            : Address.Create(input.Street, input.City, input.PostalCode);
}
