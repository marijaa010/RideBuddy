using MediatR;
using Microsoft.Extensions.Logging;
using Ride.Application.Common;
using Ride.Application.DTOs;
using Ride.Application.Interfaces;
using Ride.Domain.Entities;
using Ride.Domain.Interfaces;

namespace Ride.Application.Commands.CreateRide;

public class CreateRideCommandHandler : IRequestHandler<CreateRideCommand, Result<RideDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserGrpcClient _userClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateRideCommandHandler> _logger;

    public CreateRideCommandHandler(
        IUnitOfWork unitOfWork,
        IUserGrpcClient userClient,
        IEventPublisher eventPublisher,
        ILogger<CreateRideCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userClient = userClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<RideDto>> Handle(CreateRideCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating ride for driver {DriverId}", request.DriverId);

        // Step 1: Validate driver via gRPC
        var userInfo = await _userClient.ValidateUser(request.DriverId, cancellationToken);
        if (userInfo is null || !userInfo.IsValid)
        {
            _logger.LogWarning("Driver {DriverId} is not valid", request.DriverId);
            return Result.Failure<RideDto>("Driver not found or is not valid.");
        }

        // Step 2: Create ride
        var ride = RideEntity.Create(
            request.DriverId,
            request.OriginName, request.OriginLatitude, request.OriginLongitude,
            request.DestinationName, request.DestinationLatitude, request.DestinationLongitude,
            request.DepartureTime,
            request.AvailableSeats,
            request.PricePerSeat,
            request.Currency,
            request.AutoConfirmBookings);

        await _unitOfWork.Rides.Add(ride, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        // Step 3: Publish domain events
        await _eventPublisher.PublishMany(ride.DomainEvents, cancellationToken);
        ride.ClearDomainEvents();

        _logger.LogInformation("Ride {RideId} created successfully", ride.Id);

        return Result.Success(MapToDto(ride));
    }

    internal static RideDto MapToDto(RideEntity ride)
    {
        return new RideDto
        {
            Id = ride.Id,
            DriverId = ride.DriverId.Value,
            OriginName = ride.Origin.Name,
            OriginLatitude = ride.Origin.Latitude,
            OriginLongitude = ride.Origin.Longitude,
            DestinationName = ride.Destination.Name,
            DestinationLatitude = ride.Destination.Latitude,
            DestinationLongitude = ride.Destination.Longitude,
            DepartureTime = ride.DepartureTime,
            TotalSeats = ride.TotalSeats.Value,
            AvailableSeats = ride.AvailableSeats.Value,
            PricePerSeat = ride.PricePerSeat.Amount,
            Currency = ride.PricePerSeat.Currency,
            Status = ride.Status,
            AutoConfirmBookings = ride.AutoConfirmBookings,
            CreatedAt = ride.CreatedAt,
            StartedAt = ride.StartedAt,
            CompletedAt = ride.CompletedAt,
            CancelledAt = ride.CancelledAt,
            CancellationReason = ride.CancellationReason
        };
    }
}
