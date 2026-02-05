using Booking.Application.Common;
using MediatR;

namespace Booking.Application.Commands.ConfirmBooking;

/// <summary>
/// Command for confirming a booking.
/// Typically used internally after successful seat reservation.
/// </summary>
public record ConfirmBookingCommand : IRequest<r>
{
    public Guid BookingId { get; init; }
}
