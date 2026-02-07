using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Commands.ConfirmBooking;

/// <summary>
/// Handler for confirming a booking.
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
        _logger.LogInformation("Confirming booking {BookingId}", request.BookingId);

        var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingNotFoundException(request.BookingId);
        }

        try
        {
            booking.Confirm();
            await _unitOfWork.Bookings.UpdateAsync(booking, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishManyAsync(booking.DomainEvents, cancellationToken);
            booking.ClearDomainEvents();

            _logger.LogInformation("Booking {BookingId} successfully confirmed", request.BookingId);

            return Result.Success();
        }
        catch (BookingDomainException ex)
        {
            _logger.LogWarning(ex, "Could not confirm booking {BookingId}", request.BookingId);
            return Result.Failure(ex.Message);
        }
    }
}
