using MediatR;
using Microsoft.Extensions.Logging;
using Ride.Application.Common;
using Ride.Application.Interfaces;
using Ride.Domain.Interfaces;

namespace Ride.Application.Commands.CompleteRide;

public class CompleteRideCommandHandler : IRequestHandler<CompleteRideCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CompleteRideCommandHandler> _logger;

    public CompleteRideCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ILogger<CompleteRideCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(CompleteRideCommand request, CancellationToken cancellationToken)
    {
        var ride = await _unitOfWork.Rides.GetById(request.RideId, cancellationToken);
        if (ride is null) return Result.Failure($"Ride with ID '{request.RideId}' not found.");
        if (ride.DriverId.Value != request.DriverId) return Result.Failure("Only the driver can complete the ride.");

        ride.Complete();

        await _unitOfWork.Rides.Update(ride, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
        await _eventPublisher.PublishMany(ride.DomainEvents, cancellationToken);
        ride.ClearDomainEvents();

        _logger.LogInformation("Ride {RideId} completed", request.RideId);
        return Result.Success();
    }
}
