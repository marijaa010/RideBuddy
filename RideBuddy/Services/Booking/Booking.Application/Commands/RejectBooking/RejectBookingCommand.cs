using Booking.Application.Common;
using MediatR;

namespace Booking.Application.Commands.RejectBooking;

/// <summary>
/// Command for a driver to reject a pending booking.
/// </summary>
public record RejectBookingCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the booking to reject.
    /// </summary>
    public Guid BookingId { get; init; }

    /// <summary>
    /// ID of the driver rejecting the booking (for authorization check).
    /// </summary>
    public Guid DriverId { get; init; }

    /// <summary>
    /// Reason for rejection.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}
