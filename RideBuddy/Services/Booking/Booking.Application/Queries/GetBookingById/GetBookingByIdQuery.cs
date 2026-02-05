using Booking.Application.DTOs;
using MediatR;

namespace Booking.Application.Queries.GetBookingById;

/// <summary>
/// Query for getting a booking by its ID.
/// </summary>
public record GetBookingByIdQuery : IRequest<BookingDto?>
{
    public Guid BookingId { get; init; }
}
