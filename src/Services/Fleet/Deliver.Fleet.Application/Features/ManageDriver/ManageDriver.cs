using Deliver.Fleet.Application.Features.RegisterDriver;
using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;
using FluentValidation;
using MediatR;

namespace Deliver.Fleet.Application.Features.ManageDriver;

/// <summary>A driver starts or ends a shift.</summary>
public sealed record ChangeAvailabilityCommand(Guid DriverId, bool Available) : IRequest;

public sealed record AssignVehicleCommand(Guid DriverId, VehicleInput Vehicle) : IRequest;

internal sealed class AssignVehicleCommandValidator : AbstractValidator<AssignVehicleCommand>
{
    public AssignVehicleCommandValidator() => RuleFor(c => c.Vehicle).NotNull().SetValidator(new VehicleInputValidator());
}

internal sealed class ManageDriverHandlers(IDriverRepository drivers, IUnitOfWork unitOfWork, TimeProvider clock) :
    IRequestHandler<ChangeAvailabilityCommand>,
    IRequestHandler<AssignVehicleCommand>
{
    public async Task Handle(ChangeAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var driver = await GetAsync(command.DriverId, cancellationToken);

        if (command.Available)
            driver.GoOnline(clock.GetUtcNow());
        else
            driver.GoOffline(clock.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(AssignVehicleCommand command, CancellationToken cancellationToken)
    {
        var driver = await GetAsync(command.DriverId, cancellationToken);
        driver.AssignVehicle(command.Vehicle.ToVehicle());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Driver> GetAsync(Guid driverId, CancellationToken cancellationToken) =>
        await drivers.GetAsync(new DriverId(driverId), cancellationToken)
        ?? throw new NotFoundException("Driver", driverId);
}
