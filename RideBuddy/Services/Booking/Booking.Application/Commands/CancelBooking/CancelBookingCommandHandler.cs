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

    public async Task<r> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Cancelling booking {BookingId} by user {UserId}", 
            request.BookingId, 
            request.UserId);

        var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId, cancellationToken);

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

        var reason = string.IsNullOrWhiteSpace(request.Reason) 
            ? "Cancelled by user" 
            : request.Reason;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Cancel the booking
            booking.Cancel(reason);
            await _unitOfWork.Bookings.UpdateAsync(booking, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Release seats via gRPC
            var seatsReleased = await _rideClient.ReleaseSeatsAsync(
                booking.RideId.Value, 
                booking.SeatsBooked.Value, 
                cancellationToken);

            if (!seatsReleased)
            {
                _logger.LogWarning(
                    "Failed to release seats for booking {BookingId}, but continuing", 
                    request.BookingId);
                // Don't rollback - it's more important that the booking is cancelled
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish domain events
            await _eventPublisher.PublishManyAsync(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation("Booking {BookingId} successfully cancelled", request.BookingId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling booking {BookingId}", request.BookingId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return Result.Failure("An error occurred while cancelling the booking.");
        }
    }
}
