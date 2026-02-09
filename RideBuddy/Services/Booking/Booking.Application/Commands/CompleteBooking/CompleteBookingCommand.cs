using Booking.Application.Common;
using MediatR;

namespace Booking.Application.Commands.CompleteBooking;

/// <summary>
/// Command for marking a booking as completed after the ride is finished.
/// Only the driver of the ride can complete a booking.
/// </summary>
public record CompleteBookingCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the booking to complete.
    /// </summary>
    public Guid BookingId { get; init; }

    /// <summary>
    /// ID of the user completing the booking (must be the driver).
    /// </summary>
    public Guid UserId { get; init; }
}
