using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;

namespace Deliver.Fleet.UnitTests;

public class DriverTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Vehicle Van = Vehicle.Create("abcd12", VehicleType.Van, 800);

    private static Driver OnlineDriver()
    {
        var driver = Driver.Register("Carlos", "+56 9 1234 5678", Van, Now);
        driver.GoOnline(Now);
        driver.ClearDomainEvents();
        return driver;
    }

    [Fact]
    public void New_drivers_start_off_shift()
    {
        var driver = Driver.Register("Carlos", "+56 9 1234 5678", Van, Now);

        driver.Availability.ShouldBe(DriverAvailability.Unavailable);
        driver.Vehicle!.Plate.ShouldBe("ABCD12");
    }

    [Fact]
    public void Going_online_publishes_availability_with_vehicle_capacity()
    {
        var driver = Driver.Register("Carlos", "+56 9 1234 5678", Van, Now);

        driver.GoOnline(Now);

        driver.Availability.ShouldBe(DriverAvailability.Available);
        var available = driver.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DriverBecameAvailableDomainEvent>();
        available.VehicleCapacityKg.ShouldBe(800);
    }

    [Fact]
    public void A_driver_without_vehicle_cannot_go_online()
    {
        var driver = Driver.Register("Carlos", "+56 9 1234 5678", vehicle: null, Now);

        Should.Throw<DomainException>(() => driver.GoOnline(Now));
    }

    [Fact]
    public void Accepting_an_assignment_puts_the_driver_on_delivery()
    {
        var driver = OnlineDriver();
        var shipmentId = Guid.NewGuid();

        driver.AcceptAssignment(shipmentId, Now);

        driver.Availability.ShouldBe(DriverAvailability.OnDelivery);
        driver.CurrentShipmentId.ShouldBe(shipmentId);
        driver.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DriverBecameUnavailableDomainEvent>().Reason.ShouldBe("OnDelivery");
    }

    [Fact]
    public void Driver_cannot_be_assigned_when_unavailable()
    {
        var driver = Driver.Register("Carlos", "+56 9 1234 5678", Van, Now);
        var shipmentId = Guid.NewGuid();

        driver.AcceptAssignment(shipmentId, Now);

        driver.Availability.ShouldBe(DriverAvailability.Unavailable);
        driver.CurrentShipmentId.ShouldBeNull();
        var rejected = driver.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DriverAssignmentRejectedDomainEvent>();
        rejected.ShipmentId.ShouldBe(shipmentId);
    }

    [Fact]
    public void A_driver_on_delivery_rejects_a_second_shipment()
    {
        var driver = OnlineDriver();
        driver.AcceptAssignment(Guid.NewGuid(), Now);
        driver.ClearDomainEvents();

        driver.AcceptAssignment(Guid.NewGuid(), Now);

        driver.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DriverAssignmentRejectedDomainEvent>();
    }

    [Fact]
    public void The_same_assignment_received_twice_is_accepted_once()
    {
        var driver = OnlineDriver();
        var shipmentId = Guid.NewGuid();
        driver.AcceptAssignment(shipmentId, Now);
        driver.ClearDomainEvents();

        driver.AcceptAssignment(shipmentId, Now);

        driver.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void A_driver_cannot_go_offline_during_a_delivery()
    {
        var driver = OnlineDriver();
        driver.AcceptAssignment(Guid.NewGuid(), Now);

        Should.Throw<DomainException>(() => driver.GoOffline(Now));
    }

    [Fact]
    public void Finishing_the_current_shipment_makes_the_driver_available_again()
    {
        var driver = OnlineDriver();
        var shipmentId = Guid.NewGuid();
        driver.AcceptAssignment(shipmentId, Now);
        driver.ClearDomainEvents();

        driver.ReleaseFromShipment(shipmentId, Now);

        driver.Availability.ShouldBe(DriverAvailability.Available);
        driver.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DriverBecameAvailableDomainEvent>();
    }

    [Fact]
    public void Events_about_another_shipment_do_not_release_the_driver()
    {
        var driver = OnlineDriver();
        driver.AcceptAssignment(Guid.NewGuid(), Now);

        driver.ReleaseFromShipment(Guid.NewGuid(), Now);

        driver.Availability.ShouldBe(DriverAvailability.OnDelivery);
    }
}
