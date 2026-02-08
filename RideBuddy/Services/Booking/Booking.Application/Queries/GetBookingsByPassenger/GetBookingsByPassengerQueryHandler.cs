using Booking.Application.DTOs;
using Booking.Domain.Entities;
using Booking.Domain.Interfaces;
using MediatR;

namespace Booking.Application.Queries.GetBookingsByPassenger;

/// <summary>
/// Handler for GetBookingsByPassengerQuery.
/// </summary>
public class GetBookingsByPassengerQueryHandler 
    : IRequestHandler<GetBookingsByPassengerQuery, IReadOnlyList<BookingDto>>
{
    private readonly IBookingRepository _repository;

    public GetBookingsByPassengerQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BookingDto>> Handle(
        GetBookingsByPassengerQuery request, 
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BookingEntity> bookings;

        if (request.Status.HasValue)
        {
            bookings = await _repository.GetByPassengerAndStatus(
                request.PassengerId, 
                request.Status.Value, 
                cancellationToken);
        }
        else
        {
            bookings = await _repository.GetByPassengerId(
                request.PassengerId, 
                cancellationToken);
        }

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
