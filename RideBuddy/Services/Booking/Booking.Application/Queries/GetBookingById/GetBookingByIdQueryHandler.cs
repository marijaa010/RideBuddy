using Booking.Application.DTOs;
using Booking.Domain.Entities;
using Booking.Domain.Interfaces;
using MediatR;

namespace Booking.Application.Queries.GetBookingById;

/// <summary>
/// Handler for GetBookingByIdQuery.
/// </summary>
public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto?>
{
    private readonly IBookingRepository _repository;

    public GetBookingByIdQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDto?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _repository.GetById(request.BookingId, cancellationToken);

        return booking is null ? null : MapToDto(booking);
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
