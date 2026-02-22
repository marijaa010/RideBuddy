using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Commands.CancelBooking;

/// <summary>
/// Handler for cancelling a booking.
/// </summary>
public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRideGrpcClient _rideClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CancelBookingCommandHandler> _logger;

    public CancelBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IRideGrpcClient rideClient,
        IEventPublisher eventPublisher,
        ILogger<CancelBookingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _rideClient = rideClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Cancelling booking {BookingId} by user {UserId}", 
            request.BookingId, 
            request.UserId);

        var booking = await _unitOfWork.Bookings.GetById(request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingNotFoundException(request.BookingId);
        }

        // Check if user has permission to cancel the booking
        // (must be the passenger or the driver)
        if (booking.PassengerId.Value != request.UserId && booking.DriverId != request.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to cancel another user's booking {BookingId}", 
                request.UserId, 
                request.BookingId);
            return Result.Failure("You do not have permission to cancel this booking.");
        }

        if (!booking.CanBeCancelled())
        {
            return Result.Failure($"Booking in '{booking.Status}' status cannot be cancelled.");
        }

        var cancelledByPassenger = booking.PassengerId.Value == request.UserId;

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Cancelled by " + (cancelledByPassenger ? "passenger" : "driver")
            : request.Reason;

        var rideInfo = await _rideClient.GetRideInfo(booking.RideId.Value, cancellationToken);
        var departureTime = rideInfo?.DepartureTime ?? DateTime.MinValue;

        await _unitOfWork.BeginTransaction(cancellationToken);

        try
        {
            booking.Cancel(reason, cancelledByPassenger, departureTime);
            await _unitOfWork.Bookings.Update(booking, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            var seatsReleased = await _rideClient.ReleaseSeats(
                booking.RideId.Value, 
                booking.SeatsBooked.Value, 
                cancellationToken);

            if (!seatsReleased)
            {
                _logger.LogWarning(
                    "Failed to release seats for booking {BookingId}, but continuing", 
                    request.BookingId);
            }

            await _unitOfWork.CommitTransaction(cancellationToken);

            await _eventPublisher.PublishMany(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation("Booking {BookingId} successfully cancelled", request.BookingId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", request.BookingId);
            await _unitOfWork.RollbackTransaction(cancellationToken);
            return Result.Failure("An error occurred while cancelling the booking.");
        }
    }
}
