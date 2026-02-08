using Booking.Application.Common;
using Booking.Application.DTOs;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Commands.CreateBooking;

/// <summary>
/// Handler for creating a booking.
/// Implements the Saga pattern for coordination between services.
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<BookingDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRideGrpcClient _rideClient;
    private readonly IUserGrpcClient _userClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IRideGrpcClient rideClient,
        IUserGrpcClient userClient,
        IEventPublisher eventPublisher,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _rideClient = rideClient;
        _userClient = userClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<BookingDto>> Handle(
        CreateBookingCommand request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating booking for passenger {PassengerId} on ride {RideId}", 
            request.PassengerId, 
            request.RideId);

        // Saga Step 1: Validate user via gRPC
        var userInfo = await _userClient.ValidateUser(request.PassengerId, cancellationToken);
        if (userInfo is null || !userInfo.IsValid)
        {
            _logger.LogWarning("Passenger {PassengerId} is not valid", request.PassengerId);
            return Result.Failure<BookingDto>("User not found or is not valid.");
        }

        // Saga Step 2: Check ride availability via gRPC
        var rideInfo = await _rideClient.GetRideInfo(request.RideId, cancellationToken);
        if (rideInfo is null)
        {
            _logger.LogWarning("Ride {RideId} not found", request.RideId);
            return Result.Failure<BookingDto>("Ride not found.");
        }

        if (!rideInfo.IsAvailable)
        {
            return Result.Failure<BookingDto>("Ride is no longer available.");
        }

        if (rideInfo.AvailableSeats < request.SeatsToBook)
        {
            return Result.Failure<BookingDto>(
                $"Not enough available seats. Available: {rideInfo.AvailableSeats}");
        }

        // Saga Step 3: Check if an active booking already exists
        var existingBooking = await _unitOfWork.Bookings.ExistsActiveBooking(
            request.PassengerId, 
            request.RideId, 
            cancellationToken);

        if (existingBooking)
        {
            return Result.Failure<BookingDto>("You already have an active booking for this ride.");
        }

        // Saga Step 4: Create booking in Pending status
        var booking = BookingEntity.Create(
            request.RideId,
            request.PassengerId,
            request.SeatsToBook,
            rideInfo.PricePerSeat,
            rideInfo.Currency,
            rideInfo.DriverId);

        await _unitOfWork.BeginTransaction(cancellationToken);

        try
        {
            await _unitOfWork.Bookings.Add(booking, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            // Saga Step 5: Reserve seats via gRPC
            var seatsReserved = await _rideClient.ReserveSeats(
                request.RideId, 
                request.SeatsToBook, 
                cancellationToken);

            if (!seatsReserved)
            {
                // Compensation - rollback booking
                _logger.LogWarning(
                    "Could not reserve seats. Starting compensation for booking {BookingId}", 
                    booking.Id);
                
                booking.Reject("Could not reserve seats on the ride.");
                await _unitOfWork.Bookings.Update(booking, cancellationToken);
                await _unitOfWork.SaveChanges(cancellationToken);
                await _unitOfWork.CommitTransaction(cancellationToken);

                return Result.Failure<BookingDto>("Could not reserve seats. Please try again.");
            }

            // Saga Step 6: Confirm booking
            booking.Confirm();
            await _unitOfWork.Bookings.Update(booking, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);
            await _unitOfWork.CommitTransaction(cancellationToken);

            // Saga Step 7: Publish domain events
            await _eventPublisher.PublishMany(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation(
                "Booking {BookingId} successfully created and confirmed", 
                booking.Id);

            return Result.Success(MapToDto(booking));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking. Starting rollback.");
            
            await _unitOfWork.RollbackTransaction(cancellationToken);

            // Compensation - release seats if they were reserved
            await _rideClient.ReleaseSeats(request.RideId, request.SeatsToBook, cancellationToken);

            return Result.Failure<BookingDto>("An error occurred while creating the booking.");
        }
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
