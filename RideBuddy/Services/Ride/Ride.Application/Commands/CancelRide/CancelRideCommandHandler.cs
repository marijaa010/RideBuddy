using MediatR;
using Microsoft.Extensions.Logging;
using Ride.Application.Common;
using Ride.Application.Interfaces;
using Ride.Domain.Interfaces;

namespace Ride.Application.Commands.CancelRide;

public class CancelRideCommandHandler : IRequestHandler<CancelRideCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CancelRideCommandHandler> _logger;

    public CancelRideCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<CancelRideCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelRideCommand request, CancellationToken cancellationToken)
    {
        var ride = await _unitOfWork.Rides.GetById(request.RideId, cancellationToken);
        if (ride is null)
            return Result.Failure($"Ride with ID '{request.RideId}' not found.");

        if (ride.DriverId.Value != request.DriverId)
            return Result.Failure("Only the driver who created the ride can cancel it.");

        ride.Cancel(request.Reason);

        await _unitOfWork.Rides.Update(ride, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        await _eventPublisher.PublishMany(ride.DomainEvents, cancellationToken);
        ride.ClearDomainEvents();

        _logger.LogInformation("Ride {RideId} cancelled by driver {DriverId}", request.RideId, request.DriverId);
        return Result.Success();
    }
}
