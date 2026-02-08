using FluentValidation;

namespace Booking.Application.Commands.RejectBooking;

/// <summary>
/// Validator for RejectBookingCommand.
/// </summary>
public class RejectBookingCommandValidator : AbstractValidator<RejectBookingCommand>
{
    public RejectBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("Booking ID is required.");

        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Rejection reason cannot exceed 500 characters.");
    }
}
