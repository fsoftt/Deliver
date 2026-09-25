using Deliver.Fleet.Domain.Drivers;
using Deliver.SharedKernel;
using FluentValidation;
using MediatR;

namespace Deliver.Fleet.Application.Features.RegisterDriver;

public sealed record RegisterDriverCommand(string Name, string Phone, VehicleInput? Vehicle) : IRequest<DriverId>;

public sealed record VehicleInput(string Plate, VehicleType Type, decimal CapacityKg)
{
    public Vehicle ToVehicle() => Vehicle.Create(Plate, Type, CapacityKg);
}

internal sealed class VehicleInputValidator : AbstractValidator<VehicleInput>
{
    public VehicleInputValidator()
    {
        RuleFor(v => v.Plate).NotEmpty().MaximumLength(12);
        RuleFor(v => v.Type).IsInEnum();
        RuleFor(v => v.CapacityKg).GreaterThan(0).LessThanOrEqualTo(5000);
    }
}

internal sealed class RegisterDriverCommandValidator : AbstractValidator<RegisterDriverCommand>
{
    public RegisterDriverCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Phone).NotEmpty().MaximumLength(30);
        RuleFor(c => c.Vehicle!).SetValidator(new VehicleInputValidator()).When(c => c.Vehicle is not null);
    }
}

internal sealed class RegisterDriverHandler(IDriverRepository drivers, IUnitOfWork unitOfWork, TimeProvider clock)
    : IRequestHandler<RegisterDriverCommand, DriverId>
{
    public async Task<DriverId> Handle(RegisterDriverCommand command, CancellationToken cancellationToken)
    {
        var driver = Driver.Register(command.Name, command.Phone, command.Vehicle?.ToVehicle(), clock.GetUtcNow());
        drivers.Add(driver);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return driver.Id;
    }
}
