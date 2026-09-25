using Deliver.Dispatch.Domain.Assignments;
using Deliver.Dispatch.Domain.Drivers;
using Deliver.SharedKernel;

namespace Deliver.Dispatch.UnitTests;

public class DispatchTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Driver AvailableDriver(string name = "Carlos", decimal capacityKg = 100) =>
        Driver.FromFleet(Guid.NewGuid(), name, capacityKg, Now);

    [Fact]
    public void Assigning_a_driver_reserves_them_and_raises_DriverAssigned()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 10, Now);
        var driver = AvailableDriver();

        assignment.AssignTo(driver, Now.AddMinutes(1));

        assignment.Status.ShouldBe(AssignmentStatus.Assigned);
        assignment.DriverId.ShouldBe(driver.Id);
        driver.IsAvailable.ShouldBeFalse();
        driver.LastAssignedAt.ShouldBe(Now.AddMinutes(1));
        var assigned = assignment.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DriverAssignedDomainEvent>();
        assigned.DriverName.ShouldBe("Carlos");
    }

    [Fact]
    public void Driver_cannot_be_assigned_when_unavailable()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 10, Now);
        var driver = AvailableDriver();
        driver.MarkUnavailable(Now.AddMinutes(1));

        Should.Throw<DomainException>(() => assignment.AssignTo(driver, Now.AddMinutes(2)));
        assignment.Status.ShouldBe(AssignmentStatus.Pending);
    }

    [Fact]
    public void A_driver_cannot_take_two_shipments_at_once()
    {
        var driver = AvailableDriver();
        DeliveryAssignment.Open(Guid.NewGuid(), 10, Now).AssignTo(driver, Now);

        Should.Throw<DomainException>(() => DeliveryAssignment.Open(Guid.NewGuid(), 10, Now).AssignTo(driver, Now));
    }

    [Fact]
    public void A_vehicle_too_small_cannot_carry_the_shipment()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 250, Now);

        Should.Throw<DomainException>(() => assignment.AssignTo(AvailableDriver(capacityKg: 20), Now));
    }

    [Fact]
    public void Policy_prefers_the_driver_who_has_waited_longest_then_the_smallest_vehicle_that_fits()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 30, Now);

        var recentlyBusy = AvailableDriver("Recently busy", 40);
        DeliveryAssignment.Open(Guid.NewGuid(), 1, Now).AssignTo(recentlyBusy, Now.AddHours(-1));
        recentlyBusy.MarkAvailable("Recently busy", 40, Now);

        var bigVan = AvailableDriver("Big van", 800);
        var motorbike = AvailableDriver("Motorbike", 15);
        var smallCar = AvailableDriver("Small car", 50);

        var selected = DriverSelectionPolicy.SelectFor(assignment, [recentlyBusy, bigVan, motorbike, smallCar]);

        selected.ShouldBe(smallCar);
    }

    [Fact]
    public void Policy_returns_nobody_when_no_driver_fits()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 30, Now);

        DriverSelectionPolicy.SelectFor(assignment, [AvailableDriver(capacityKg: 10)]).ShouldBeNull();
    }

    [Fact]
    public void A_rejected_driver_is_never_offered_the_same_shipment_again()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 10, Now);
        var carlos = AvailableDriver("Carlos");
        var maria = AvailableDriver("Maria");
        assignment.AssignTo(carlos, Now);

        assignment.RejectDriver(carlos.Id);
        carlos.MarkAvailable("Carlos", 100, Now.AddMinutes(5));

        assignment.Status.ShouldBe(AssignmentStatus.Pending);
        assignment.RejectedDriverIds.ShouldContain(carlos.Id);
        DriverSelectionPolicy.SelectFor(assignment, [carlos, maria]).ShouldBe(maria);
    }

    [Fact]
    public void Out_of_order_availability_facts_are_ignored()
    {
        var driver = AvailableDriver();
        driver.MarkUnavailable(Now.AddMinutes(10));

        driver.MarkAvailable("Carlos", 100, Now.AddMinutes(5)); // older fact, delivered late

        driver.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Cancelling_is_idempotent_and_completed_assignments_stay_completed()
    {
        var assignment = DeliveryAssignment.Open(Guid.NewGuid(), 10, Now);
        assignment.AssignTo(AvailableDriver(), Now);
        assignment.Complete();

        assignment.Cancel();

        assignment.Status.ShouldBe(AssignmentStatus.Completed);
    }
}
