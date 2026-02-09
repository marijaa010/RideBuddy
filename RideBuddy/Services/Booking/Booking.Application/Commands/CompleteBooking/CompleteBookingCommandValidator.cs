using FluentValidation;

namespace Booking.Application.Commands.CompleteBooking;

/// <summary>
/// Validator for CompleteBookingCommand.
/// </summary>
public class CompleteBookingCommandValidator : AbstractValidator<CompleteBookingCommand>
{
    public CompleteBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
