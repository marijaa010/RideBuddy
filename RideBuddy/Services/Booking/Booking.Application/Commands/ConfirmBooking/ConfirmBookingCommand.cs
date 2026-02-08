using Booking.Application.Common;
using MediatR;

namespace Booking.Application.Commands.ConfirmBooking;

/// <summary>
/// Command for confirming a booking.
/// Used by the driver to manually approve a pending booking.
/// </summary>
public record ConfirmBookingCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the booking to confirm.
    /// </summary>
    public Guid BookingId { get; init; }

    /// <summary>
    /// ID of the user confirming (must be the driver of the ride).
    /// </summary>
    public Guid UserId { get; init; }
}
