using Deliver.SharedKernel;
using Deliver.Shipping.Domain.Shipments;

namespace Deliver.Shipping.UnitTests;

public class ShipmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DriverId Carlos = new(Guid.NewGuid());

    private static Shipment NewShipment() => Shipment.Create(
        CustomerId.From(Guid.NewGuid()),
        Address.Create("Av. Providencia 1234", "Santiago", "7500000"),
        Address.Create("Calle Valparaíso 55", "Viña del Mar", "2520000"),
        [ShipmentItem.Create("Books", 2, 1.5m), ShipmentItem.Create("Laptop", 1, 2.25m)],
        Now);

    private static Shipment ShipmentIn(ShipmentStatus status)
    {
        var shipment = NewShipment();
        if (status == ShipmentStatus.Created) return shipment;
        shipment.AssignDriver(Carlos, Now);
        if (status == ShipmentStatus.Assigned) return shipment;
        shipment.MarkAsPickedUp(Now);
        if (status == ShipmentStatus.PickedUp) return shipment;
        shipment.MarkAsInTransit(Now);
        if (status == ShipmentStatus.InTransit) return shipment;
        shipment.MarkAsDelivered(Now);
        return shipment;
    }

    [Fact]
    public void Creating_a_shipment_raises_ShipmentCreated_with_the_total_weight()
    {
        var shipment = NewShipment();

        shipment.Status.ShouldBe(ShipmentStatus.Created);
        shipment.Price.ShouldBeNull();
        var created = shipment.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ShipmentCreatedDomainEvent>();
        created.TotalWeight.Kilograms.ShouldBe(5.25m);
        created.ItemCount.ShouldBe(2);
    }

    [Fact]
    public void A_shipment_needs_at_least_one_item()
    {
        Should.Throw<InvalidValueException>(() => Shipment.Create(
            CustomerId.From(Guid.NewGuid()),
            Address.Create("A 1", "Santiago", "1"),
            Address.Create("B 2", "Santiago", "2"),
            [],
            Now));
    }

    [Fact]
    public void Pickup_and_delivery_addresses_must_differ()
    {
        var address = Address.Create("A 1", "Santiago", "1");

        Should.Throw<InvalidValueException>(() => Shipment.Create(
            CustomerId.From(Guid.NewGuid()), address, address, [ShipmentItem.Create("Box", 1, 1)], Now));
    }

    [Fact]
    public void Goes_through_the_whole_lifecycle_raising_one_event_per_transition()
    {
        var shipment = NewShipment();
        shipment.ClearDomainEvents();

        shipment.AssignDriver(Carlos, Now);
        shipment.MarkAsPickedUp(Now.AddMinutes(10));
        shipment.MarkAsInTransit(Now.AddMinutes(11));
        shipment.MarkAsDelivered(Now.AddMinutes(40));

        shipment.Status.ShouldBe(ShipmentStatus.Delivered);
        shipment.DeliveredAt.ShouldBe(Now.AddMinutes(40));
        shipment.DomainEvents.Select(e => e.GetType()).ShouldBe(
        [
            typeof(DriverAssignedDomainEvent),
            typeof(ShipmentPickedUpDomainEvent),
            typeof(ShipmentInTransitDomainEvent),
            typeof(ShipmentDeliveredDomainEvent),
        ]);
    }

    [Fact]
    public void Shipment_cannot_be_picked_up_before_assignment()
    {
        var shipment = ShipmentIn(ShipmentStatus.Created);

        Should.Throw<DomainException>(() => shipment.MarkAsPickedUp(Now));
        shipment.Status.ShouldBe(ShipmentStatus.Created);
    }

    [Theory]
    [InlineData(ShipmentStatus.Created)]
    [InlineData(ShipmentStatus.Assigned)]
    [InlineData(ShipmentStatus.PickedUp)]
    public void Shipment_cannot_be_delivered_before_it_is_in_transit(ShipmentStatus status)
    {
        var shipment = ShipmentIn(status);

        Should.Throw<DomainException>(() => shipment.MarkAsDelivered(Now));
        shipment.Status.ShouldBe(status);
    }

    [Theory]
    [InlineData(ShipmentStatus.PickedUp)]
    [InlineData(ShipmentStatus.InTransit)]
    [InlineData(ShipmentStatus.Delivered)]
    public void Shipment_cannot_be_cancelled_once_the_driver_has_it(ShipmentStatus status)
    {
        var shipment = ShipmentIn(status);

        Should.Throw<DomainException>(() => shipment.Cancel("Changed my mind", Now));
    }

    [Fact]
    public void Cancelling_an_assigned_shipment_tells_who_the_driver_was()
    {
        var shipment = ShipmentIn(ShipmentStatus.Assigned);
        shipment.ClearDomainEvents();

        shipment.Cancel("Customer request", Now);

        shipment.Status.ShouldBe(ShipmentStatus.Cancelled);
        var cancelled = shipment.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ShipmentCancelledDomainEvent>();
        cancelled.DriverId.ShouldBe(Carlos);
    }

    [Fact]
    public void A_cancellation_needs_a_reason()
    {
        Should.Throw<InvalidValueException>(() => NewShipment().Cancel(" ", Now));
    }

    [Fact]
    public void Driver_can_be_reassigned_before_pickup()
    {
        var shipment = ShipmentIn(ShipmentStatus.Assigned);
        var maria = new DriverId(Guid.NewGuid());

        shipment.AssignDriver(maria, Now);

        shipment.DriverId.ShouldBe(maria);
    }

    [Fact]
    public void Receiving_the_same_assignment_twice_changes_nothing()
    {
        var shipment = ShipmentIn(ShipmentStatus.Assigned);
        shipment.ClearDomainEvents();

        shipment.AssignDriver(Carlos, Now.AddMinutes(5));

        shipment.DomainEvents.ShouldBeEmpty();
        shipment.AssignedAt.ShouldBe(Now);
    }

    [Fact]
    public void Driver_cannot_be_assigned_after_pickup()
    {
        var shipment = ShipmentIn(ShipmentStatus.PickedUp);

        Should.Throw<DomainException>(() => shipment.AssignDriver(new DriverId(Guid.NewGuid()), Now));
    }

    [Fact]
    public void Price_arrives_later_and_can_be_quoted_in_any_state()
    {
        var shipment = ShipmentIn(ShipmentStatus.Assigned);

        shipment.QuotePrice(new Money(12500, "clp"));

        shipment.Price.ShouldBe(new Money(12500, "CLP"));
    }

    [Fact]
    public void Payment_failure_is_recorded_once_and_only_for_delivered_shipments()
    {
        Should.Throw<DomainException>(() => ShipmentIn(ShipmentStatus.InTransit).RecordPaymentFailed("Declined", Now));

        var delivered = ShipmentIn(ShipmentStatus.Delivered);
        delivered.ClearDomainEvents();

        delivered.RecordPaymentFailed("Declined", Now);
        delivered.RecordPaymentFailed("Declined", Now);

        delivered.PaymentStatus.ShouldBe(PaymentStatus.Failed);
        delivered.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ShipmentDeliveryPaymentFailedDomainEvent>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Weight_must_be_positive(decimal kilograms)
    {
        Should.Throw<InvalidValueException>(() => new Weight(kilograms));
    }

    [Theory]
    [InlineData(null, "Santiago", "1")]
    [InlineData("Street 1", "", "1")]
    [InlineData("Street 1", "Santiago", " ")]
    public void Address_requires_street_city_and_postal_code(string? street, string? city, string? postalCode)
    {
        Should.Throw<InvalidValueException>(() => Address.Create(street, city, postalCode));
    }
}
