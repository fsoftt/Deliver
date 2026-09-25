using Deliver.Fleet.Application.Features.RegisterDriver;
using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;

namespace Deliver.Fleet.Application.Features.ManageDriver;

public sealed record ChangeAvailabilityCommand(Guid DriverId, bool Available);

/// <summary>A driver starts or ends a shift.</summary>
public sealed class ChangeAvailabilityHandler(IDriverRepository drivers, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task HandleAsync(ChangeAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetAsync(new DriverId(command.DriverId), cancellationToken)
            ?? throw new NotFoundException("Driver", command.DriverId);

        if (command.Available)
            driver.GoOnline(clock.GetUtcNow());
        else
            driver.GoOffline(clock.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed record AssignVehicleCommand(Guid DriverId, VehicleInput Vehicle);

public sealed class AssignVehicleHandler(IDriverRepository drivers, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(AssignVehicleCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.GetAsync(new DriverId(command.DriverId), cancellationToken)
            ?? throw new NotFoundException("Driver", command.DriverId);

        driver.AssignVehicle(command.Vehicle.ToVehicle());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
