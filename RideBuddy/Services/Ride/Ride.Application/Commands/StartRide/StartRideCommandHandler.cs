using MediatR;
using Microsoft.Extensions.Logging;
using Ride.Application.Common;
using Ride.Application.Interfaces;
using Ride.Domain.Enums;
using Ride.Domain.Interfaces;

namespace Ride.Application.Commands.StartRide;

public class StartRideCommandHandler : IRequestHandler<StartRideCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<StartRideCommandHandler> _logger;

    public StartRideCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ILogger<StartRideCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(StartRideCommand request, CancellationToken cancellationToken)
    {
        var ride = await _unitOfWork.Rides.GetById(request.RideId, cancellationToken);
        if (ride is null) return Result.Failure($"Ride with ID '{request.RideId}' not found.");
        if (ride.DriverId.Value != request.DriverId) return Result.Failure("Only the driver can start the ride.");
        if (ride.Status != RideStatus.Scheduled)
            return Result.Failure($"Ride can be started only when status is 'Scheduled'. Current status: '{ride.Status}'.");
        if (DateTime.UtcNow < ride.DepartureTime)
            return Result.Failure("Ride cannot be started before the scheduled departure time.");

        ride.Start();

        await _unitOfWork.Rides.Update(ride, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
        await _eventPublisher.PublishMany(ride.DomainEvents, cancellationToken);
        ride.ClearDomainEvents();

        _logger.LogInformation("Ride {RideId} started", request.RideId);
        return Result.Success();
    }
}
