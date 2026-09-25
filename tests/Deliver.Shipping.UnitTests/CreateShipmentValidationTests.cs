using Deliver.Application.Pipeline;
using Deliver.SharedKernel;
using Deliver.Shipping.Application.Features.CreateShipment;
using Deliver.Shipping.Domain.Shipments;
using MediatR;

namespace Deliver.Shipping.UnitTests;

public class CreateShipmentValidationTests
{
    private static CreateShipmentCommand ValidCommand() => new(
        Guid.NewGuid(),
        new AddressInput("Av. Providencia 1234", "Santiago", "7500000"),
        new AddressInput("Calle Valparaíso 55", "Viña del Mar", "2520000"),
        [new ItemInput("Books", 2, 1.5m)]);

    [Fact]
    public void A_well_formed_command_is_valid()
    {
        new CreateShipmentCommandValidator().Validate(ValidCommand()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        var command = ValidCommand() with
        {
            CustomerId = Guid.Empty,
            PickupAddress = new AddressInput("", "Santiago", "7500000"),
            Items = [new ItemInput("Books", 0, -1)],
        };

        var result = new CreateShipmentCommandValidator().Validate(command);

        result.Errors.Select(e => e.PropertyName).ShouldBe(
            ["CustomerId", "PickupAddress.Street", "Items[0].Quantity", "Items[0].UnitWeightKg"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task The_pipeline_stops_invalid_commands_before_the_handler_runs()
    {
        var behavior = new ValidationBehavior<CreateShipmentCommand, ShipmentId>([new CreateShipmentCommandValidator()]);
        var handlerCalled = false;
        RequestHandlerDelegate<ShipmentId> handler = _ =>
        {
            handlerCalled = true;
            return Task.FromResult(ShipmentId.New());
        };

        var exception = await Should.ThrowAsync<RequestValidationException>(() =>
            behavior.Handle(ValidCommand() with { Items = [] }, handler, CancellationToken.None));

        exception.Errors.ShouldContainKey("Items");
        handlerCalled.ShouldBeFalse();
    }
}
