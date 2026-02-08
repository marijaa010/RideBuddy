using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Commands.RejectBooking;

/// <summary>
/// Handler for a driver rejecting a pending booking.
/// Releases the reserved seats and notifies the passenger via domain event.
/// </summary>
public class RejectBookingCommandHandler : IRequestHandler<RejectBookingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRideGrpcClient _rideClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RejectBookingCommandHandler> _logger;

    public RejectBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IRideGrpcClient rideClient,
        IEventPublisher eventPublisher,
        ILogger<RejectBookingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _rideClient = rideClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Driver {DriverId} rejecting booking {BookingId}",
            request.DriverId,
            request.BookingId);

        var booking = await _unitOfWork.Bookings.GetById(request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingNotFoundException(request.BookingId);
        }

        // Only the driver of the ride can reject
        if (booking.DriverId != request.DriverId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to reject booking {BookingId} but is not the driver",
                request.DriverId,
                request.BookingId);
            return Result.Failure("Only the driver can reject a booking.");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Rejected by driver"
            : request.Reason;

        await _unitOfWork.BeginTransaction(cancellationToken);

        try
        {
            booking.Reject(reason);
            await _unitOfWork.Bookings.Update(booking, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            // Release the reserved seats
            var seatsReleased = await _rideClient.ReleaseSeats(
                booking.RideId.Value,
                booking.SeatsBooked.Value,
                cancellationToken);

            if (!seatsReleased)
            {
                _logger.LogWarning(
                    "Failed to release seats for rejected booking {BookingId}, but continuing",
                    request.BookingId);
            }

            await _unitOfWork.CommitTransaction(cancellationToken);

            // Publish domain events (BookingRejectedEvent → Notification Service)
            await _eventPublisher.PublishMany(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation("Booking {BookingId} rejected by driver", request.BookingId);

            return Result.Success();
        }
        catch (BookingDomainException ex)
        {
            _logger.LogWarning(ex, "Could not reject booking {BookingId}", request.BookingId);
            await _unitOfWork.RollbackTransaction(cancellationToken);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting booking {BookingId}", request.BookingId);
            await _unitOfWork.RollbackTransaction(cancellationToken);
            return Result.Failure("An error occurred while rejecting the booking.");
        }
    }
}
