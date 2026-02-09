using MediatR;
using Microsoft.Extensions.Logging;
using Ride.Application.Common;
using Ride.Application.Interfaces;
using Ride.Domain.Interfaces;

namespace Ride.Application.Commands.ReserveSeats;

public class ReserveSeatsCommandHandler : IRequestHandler<ReserveSeatsCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ReserveSeatsCommandHandler> _logger;

    public ReserveSeatsCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ILogger<ReserveSeatsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ReserveSeatsCommand request, CancellationToken cancellationToken)
    {
        var ride = await _unitOfWork.Rides.GetById(request.RideId, cancellationToken);
        if (ride is null) return Result.Failure($"Ride with ID '{request.RideId}' not found.");

        ride.ReserveSeats(request.SeatsCount);

        await _unitOfWork.Rides.Update(ride, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
        await _eventPublisher.PublishMany(ride.DomainEvents, cancellationToken);
        ride.ClearDomainEvents();

        _logger.LogInformation("Reserved {Count} seats on ride {RideId}", request.SeatsCount, request.RideId);
        return Result.Success();
    }
}
