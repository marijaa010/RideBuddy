using MediatR;
using Microsoft.Extensions.Logging;
using Ride.Application.Common;
using Ride.Application.Interfaces;
using Ride.Domain.Interfaces;

namespace Ride.Application.Commands.ReleaseSeats;

public class ReleaseSeatsCommandHandler : IRequestHandler<ReleaseSeatsCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ReleaseSeatsCommandHandler> _logger;

    public ReleaseSeatsCommandHandler(IUnitOfWork unitOfWork, IEventPublisher eventPublisher, ILogger<ReleaseSeatsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result> Handle(ReleaseSeatsCommand request, CancellationToken cancellationToken)
    {
        var ride = await _unitOfWork.Rides.GetById(request.RideId, cancellationToken);
        if (ride is null) return Result.Failure($"Ride with ID '{request.RideId}' not found.");

        ride.ReleaseSeats(request.SeatsCount);

        await _unitOfWork.Rides.Update(ride, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
        await _eventPublisher.PublishMany(ride.DomainEvents, cancellationToken);
        ride.ClearDomainEvents();

        _logger.LogInformation("Released {Count} seats on ride {RideId}", request.SeatsCount, request.RideId);
        return Result.Success();
    }
}
