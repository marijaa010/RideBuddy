using FluentValidation;

namespace Ride.Application.Commands.CreateRide;

public class CreateRideCommandValidator : AbstractValidator<CreateRideCommand>
{
    public CreateRideCommandValidator()
    {
        RuleFor(x => x.DriverId).NotEmpty().WithMessage("Driver ID is required.");
        RuleFor(x => x.OriginName).NotEmpty().WithMessage("Origin name is required.");
        RuleFor(x => x.OriginLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.OriginLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DestinationName).NotEmpty().WithMessage("Destination name is required.");
        RuleFor(x => x.DestinationLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.DestinationLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DepartureTime).GreaterThan(DateTime.UtcNow).WithMessage("Departure time must be in the future.");
        RuleFor(x => x.AvailableSeats).GreaterThan(0).LessThanOrEqualTo(8).WithMessage("Seats must be between 1 and 8.");
        RuleFor(x => x.PricePerSeat).GreaterThan(0).WithMessage("Price per seat must be greater than 0.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter code.");
    }
}
