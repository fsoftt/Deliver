using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;

namespace Deliver.Fleet.Application.Features.RegisterDriver;

public sealed record RegisterDriverCommand(string Name, string Phone, VehicleInput? Vehicle);

public sealed record VehicleInput(string Plate, VehicleType Type, decimal CapacityKg)
{
    public Vehicle ToVehicle() => Vehicle.Create(Plate, Type, CapacityKg);
}

public sealed class RegisterDriverHandler(IDriverRepository drivers, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<DriverId> HandleAsync(RegisterDriverCommand command, CancellationToken cancellationToken)
    {
        var driver = Driver.Register(command.Name, command.Phone, command.Vehicle?.ToVehicle(), clock.GetUtcNow());
        drivers.Add(driver);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return driver.Id;
    }
}
