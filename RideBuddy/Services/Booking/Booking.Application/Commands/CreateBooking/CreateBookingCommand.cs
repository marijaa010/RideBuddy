using Booking.Application.Common;
using Booking.Application.DTOs;
using MediatR;

namespace Booking.Application.Commands.CreateBooking;

/// <summary>
/// Command for creating a new booking.
/// </summary>
public record CreateBookingCommand : IRequest<Result<BookingDto>>
{
    /// <summary>
    /// ID of the ride to book.
    /// </summary>
    public Guid RideId { get; init; }

    /// <summary>
    /// ID of the passenger making the booking.
    /// </summary>
    public Guid PassengerId { get; init; }

    /// <summary>
    /// Number of seats to book.
    /// </summary>
    public int SeatsToBook { get; init; }
}
