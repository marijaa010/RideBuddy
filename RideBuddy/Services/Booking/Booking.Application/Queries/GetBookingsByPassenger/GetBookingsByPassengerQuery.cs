using Booking.Application.DTOs;
using Booking.Domain.Enums;
using MediatR;

namespace Booking.Application.Queries.GetBookingsByPassenger;

/// <summary>
/// Query for getting all bookings for a specific passenger.
/// </summary>
public record GetBookingsByPassengerQuery : IRequest<IReadOnlyList<BookingDto>>
{
    public Guid PassengerId { get; init; }
    
    /// <summary>
    /// Optional status filter.
    /// </summary>
    public BookingStatus? Status { get; init; }
}
