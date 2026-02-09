using FluentValidation;

namespace Ride.Application.Commands.CancelRide;

public class CancelRideCommandValidator : AbstractValidator<CancelRideCommand>
{
    public CancelRideCommandValidator()
    {
        RuleFor(x => x.RideId).NotEmpty().WithMessage("Ride ID is required.");
        RuleFor(x => x.DriverId).NotEmpty().WithMessage("Driver ID is required.");
    }
}
