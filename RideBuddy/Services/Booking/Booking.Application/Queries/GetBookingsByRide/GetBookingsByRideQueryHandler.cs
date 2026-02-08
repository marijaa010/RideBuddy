using Booking.Application.DTOs;
using Booking.Domain.Entities;
using Booking.Domain.Interfaces;
using MediatR;

namespace Booking.Application.Queries.GetBookingsByRide;

/// <summary>
/// Handler for GetBookingsByRideQuery.
/// </summary>
public class GetBookingsByRideQueryHandler 
    : IRequestHandler<GetBookingsByRideQuery, IReadOnlyList<BookingDto>>
{
    private readonly IBookingRepository _repository;

    public GetBookingsByRideQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BookingDto>> Handle(
        GetBookingsByRideQuery request, 
        CancellationToken cancellationToken)
    {
        var bookings = await _repository.GetByRideId(request.RideId, cancellationToken);

        return bookings.Select(MapToDto).ToList();
    }

    private static BookingDto MapToDto(BookingEntity booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            RideId = booking.RideId.Value,
            PassengerId = booking.PassengerId.Value,
            DriverId = booking.DriverId,
            SeatsBooked = booking.SeatsBooked.Value,
            TotalPrice = booking.TotalPrice.Amount,
            Currency = booking.TotalPrice.Currency,
            Status = booking.Status,
            BookedAt = booking.BookedAt,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt,
            CompletedAt = booking.CompletedAt,
            CancellationReason = booking.CancellationReason
        };
    }
}
