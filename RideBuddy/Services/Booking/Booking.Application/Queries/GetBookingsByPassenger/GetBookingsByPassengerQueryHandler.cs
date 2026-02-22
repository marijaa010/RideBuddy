using Booking.Application.DTOs;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Queries.GetBookingsByPassenger;

/// <summary>
/// Handler for GetBookingsByPassengerQuery.
/// </summary>
public class GetBookingsByPassengerQueryHandler 
    : IRequestHandler<GetBookingsByPassengerQuery, IReadOnlyList<BookingDto>>
{
    private readonly IBookingRepository _repository;
    private readonly IRideGrpcClient _rideClient;
    private readonly ILogger<GetBookingsByPassengerQueryHandler> _logger;

    public GetBookingsByPassengerQueryHandler(
        IBookingRepository repository,
        IRideGrpcClient rideClient,
        ILogger<GetBookingsByPassengerQueryHandler> logger)
    {
        _repository = repository;
        _rideClient = rideClient;
        _logger = logger;
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

        var bookingDtos = new List<BookingDto>();
        foreach (var booking in bookings)
        {
            var dto = MapToDto(booking);
            
            try
            {
                var rideInfo = await _rideClient.GetRideInfo(booking.RideId.Value, cancellationToken);
                dto = dto with { Ride = rideInfo };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch ride info for ride {RideId}", booking.RideId.Value);
            }
            
            bookingDtos.Add(dto);
        }

        return bookingDtos;
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
