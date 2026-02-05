using FluentValidation;

namespace Booking.Application.Commands.CreateBooking;

/// <summary>
/// Validator for CreateBookingCommand.
/// </summary>
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.RideId)
            .NotEmpty()
            .WithMessage("Ride ID is required.");

        RuleFor(x => x.PassengerId)
            .NotEmpty()
            .WithMessage("Passenger ID is required.");

        RuleFor(x => x.SeatsToBook)
            .GreaterThan(0)
            .WithMessage("Number of seats must be greater than 0.")
            .LessThanOrEqualTo(8)
            .WithMessage("Cannot book more than 8 seats.");
    }
}
