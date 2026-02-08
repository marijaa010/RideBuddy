using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Commands.ConfirmBooking;

/// <summary>
/// Handler for confirming a booking.
/// Used by the driver to manually approve a pending booking.
/// </summary>
public class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ConfirmBookingCommandHandler> _logger;

    public ConfirmBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<ConfirmBookingCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} confirming booking {BookingId}",
            request.UserId,
            request.BookingId);

        var booking = await _unitOfWork.Bookings.GetById(request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingNotFoundException(request.BookingId);
        }

        if (booking.DriverId != request.UserId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to confirm booking {BookingId} but is not the driver",
                request.UserId,
                request.BookingId);
            return Result.Failure("Only the driver can confirm a booking.");
        }

        try
        {
            booking.Confirm();
            await _unitOfWork.Bookings.Update(booking, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            await _eventPublisher.PublishMany(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation("Booking {BookingId} confirmed by driver", request.BookingId);

            return Result.Success();
        }
        catch (BookingDomainException ex)
        {
            _logger.LogWarning(ex, "Could not confirm booking {BookingId}", request.BookingId);
            return Result.Failure(ex.Message);
        }
    }
}
