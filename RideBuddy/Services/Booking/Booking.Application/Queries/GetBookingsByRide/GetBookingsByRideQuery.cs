using Booking.Application.DTOs;
using MediatR;

namespace Booking.Application.Queries.GetBookingsByRide;

/// <summary>
/// Query for getting all bookings for a specific ride.
/// </summary>
public record GetBookingsByRideQuery : IRequest<IReadOnlyList<BookingDto>>
{
    public Guid RideId { get; init; }
}
