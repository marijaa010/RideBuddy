using Booking.Application.Common;
using MediatR;

namespace Booking.Application.Commands.CancelBooking;

/// <summary>
/// Command for cancelling a booking.
/// </summary>
public record CancelBookingCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the booking to cancel.
    /// </summary>
    public Guid BookingId { get; init; }

    /// <summary>
    /// ID of the user cancelling (for authorization check).
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}
