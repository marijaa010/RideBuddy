using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ride.Domain.Interfaces;
using Ride.Infrastructure.Protos;

namespace Ride.Infrastructure.Services;

/// <summary>
/// gRPC server implementation for the Ride service.
/// Handles requests from Booking Service to check availability, reserve/release seats.
/// </summary>
public class RideGrpcService : RideGrpc.RideGrpcBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RideGrpcService> _logger;

    public RideGrpcService(IUnitOfWork unitOfWork, ILogger<RideGrpcService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves complete ride information including availability, price, and driver details.
    /// Called by Booking Service during booking creation to verify ride availability.
    /// Throws RpcException if ride not found.
    /// </summary>
    /// <param name="request">Request containing ride ID to fetch</param>
    /// <param name="context">gRPC server call context with cancellation token</param>
    /// <returns>RideInfoResponse with complete ride details and availability status</returns>
    /// <exception cref="RpcException">Thrown if ride ID is invalid or ride not found</exception>
    public override async Task<RideInfoResponse> GetRideInfo(
        GetRideInfoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetRideInfo called for {RideId}", request.RideId);

        if (!Guid.TryParse(request.RideId, out var rideId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid ride ID."));
        }

        var ride = await _unitOfWork.Rides.GetById(rideId, context.CancellationToken);
        if (ride is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Ride not found."));
        }

        return new RideInfoResponse
        {
            RideId = ride.Id.ToString(),
            DriverId = ride.DriverId.Value.ToString(),
            Origin = ride.Origin.Name,
            Destination = ride.Destination.Name,
            DepartureTime = ride.DepartureTime.ToString("O"),
            AvailableSeats = ride.AvailableSeats.Value,
            PricePerSeat = (double)ride.PricePerSeat.Amount,
            Currency = ride.PricePerSeat.Currency,
            IsAvailable = ride.IsAvailable,
            AutoConfirmBookings = ride.AutoConfirmBookings
        };
    }

    /// <summary>
    /// Checks if a ride has enough available seats for booking request.
    /// Returns availability status without modifying ride state.
    /// Used for real-time availability checks before reservation.
    /// </summary>
    /// <param name="request">Request containing ride ID and number of seats requested</param>
    /// <param name="context">gRPC server call context with cancellation token</param>
    /// <returns>CheckAvailabilityResponse with availability flag and current seat count</returns>
    public override async Task<CheckAvailabilityResponse> CheckAvailability(
        CheckAvailabilityRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CheckAvailability for ride {RideId}, seats: {Seats}",
            request.RideId, request.SeatsRequested);

        if (!Guid.TryParse(request.RideId, out var rideId))
        {
            return new CheckAvailabilityResponse { IsAvailable = false, AvailableSeats = 0 };
        }

        var ride = await _unitOfWork.Rides.GetById(rideId, context.CancellationToken);
        if (ride is null)
        {
            return new CheckAvailabilityResponse { IsAvailable = false, AvailableSeats = 0 };
        }

        return new CheckAvailabilityResponse
        {
            IsAvailable = ride.IsAvailable && ride.AvailableSeats.Value >= request.SeatsRequested,
            AvailableSeats = ride.AvailableSeats.Value
        };
    }

    /// <summary>
    /// Reserves seats on a ride (transactional operation).
    /// Called by Booking Service during booking creation (orchestration step).
    /// Uses optimistic concurrency control via Version property to prevent overbooking.
    /// Returns success flag instead of throwing to allow compensation logic in caller.
    /// </summary>
    /// <param name="request">Request containing ride ID and number of seats to reserve</param>
    /// <param name="context">gRPC server call context with cancellation token</param>
    /// <returns>ReserveSeatsResponse with success flag and message</returns>
    public override async Task<ReserveSeatsResponse> ReserveSeats(
        ReserveSeatsRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ReserveSeats for ride {RideId}, count: {Count}",
            request.RideId, request.SeatsCount);

        if (!Guid.TryParse(request.RideId, out var rideId))
        {
            return new ReserveSeatsResponse { Success = false, Message = "Invalid ride ID." };
        }

        var ride = await _unitOfWork.Rides.GetById(rideId, context.CancellationToken);
        if (ride is null)
        {
            return new ReserveSeatsResponse { Success = false, Message = "Ride not found." };
        }

        try
        {
            ride.ReserveSeats(request.SeatsCount);
            await _unitOfWork.Rides.Update(ride, context.CancellationToken);
            await _unitOfWork.SaveChanges(context.CancellationToken);

            _logger.LogInformation("Reserved {Count} seats on ride {RideId}. Remaining: {Remaining}",
                request.SeatsCount, rideId, ride.AvailableSeats.Value);

            return new ReserveSeatsResponse { Success = true, Message = "Seats reserved successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reserve seats on ride {RideId}", rideId);
            return new ReserveSeatsResponse { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Releases previously reserved seats back to availability.
    /// Called by Booking Service during compensation/rollback (when booking fails or is cancelled).
    /// Part of the Saga pattern for distributed transaction management.
    /// </summary>
    /// <param name="request">Request containing ride ID and number of seats to release</param>
    /// <param name="context">gRPC server call context with cancellation token</param>
    /// <returns>ReleaseSeatsResponse with success flag and message</returns>
    public override async Task<ReleaseSeatsResponse> ReleaseSeats(
        ReleaseSeatsRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ReleaseSeats for ride {RideId}, count: {Count}",
            request.RideId, request.SeatsCount);

        if (!Guid.TryParse(request.RideId, out var rideId))
        {
            return new ReleaseSeatsResponse { Success = false, Message = "Invalid ride ID." };
        }

        var ride = await _unitOfWork.Rides.GetById(rideId, context.CancellationToken);
        if (ride is null)
        {
            return new ReleaseSeatsResponse { Success = false, Message = "Ride not found." };
        }

        try
        {
            ride.ReleaseSeats(request.SeatsCount);
            await _unitOfWork.Rides.Update(ride, context.CancellationToken);
            await _unitOfWork.SaveChanges(context.CancellationToken);

            _logger.LogInformation("Released {Count} seats on ride {RideId}. Available: {Available}",
                request.SeatsCount, rideId, ride.AvailableSeats.Value);

            return new ReleaseSeatsResponse { Success = true, Message = "Seats released successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release seats on ride {RideId}", rideId);
            return new ReleaseSeatsResponse { Success = false, Message = ex.Message };
        }
    }
}
