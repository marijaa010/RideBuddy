using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Commands.CompleteBooking;

/// <summary>
/// Handler for completing a booking after the ride is finished.
/// Only the driver can mark a booking as completed.
/// </summary>
public class CompleteBookingCommandHandler : IRequestHandler<CompleteBookingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CompleteBookingCommandHandler> _logger;

    public CompleteBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<CompleteBookingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} completing booking {BookingId}",
            request.UserId,
            request.BookingId);

        var booking = await _unitOfWork.Bookings.GetById(request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingNotFoundException(request.BookingId);
        }

        // Only the driver of the ride can complete a booking
        if (booking.DriverId != request.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to complete booking {BookingId} but is not the driver",
                request.UserId,
                request.BookingId);
            return Result.Failure("Only the driver can complete a booking.");
        }

        try
        {
            booking.Complete();
            await _unitOfWork.Bookings.Update(booking, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            await _eventPublisher.PublishMany(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation("Booking {BookingId} completed by driver", request.BookingId);

            return Result.Success();
        }
        catch (BookingDomainException ex)
        {
            _logger.LogWarning(ex, "Could not complete booking {BookingId}", request.BookingId);
            return Result.Failure(ex.Message);
        }
    }
}
