using Booking.Application.DTOs;
using Booking.Application.Interfaces;
using Booking.Infrastructure.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Booking.Infrastructure.Services;

/// <summary>
/// gRPC client for communication with Ride Service.
/// </summary>
public class RideGrpcClient : IRideGrpcClient
{
    private readonly RideGrpc.RideGrpcClient _client;
    private readonly ILogger<RideGrpcClient> _logger;

    public RideGrpcClient(RideGrpc.RideGrpcClient client, ILogger<RideGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<RideInfoDto?> GetRideInfoAsync(Guid rideId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetRideInfoRequest { RideId = rideId.ToString() };
            var response = await _client.GetRideInfoAsync(request, cancellationToken: cancellationToken);

            return new RideInfoDto
            {
                RideId = Guid.Parse(response.RideId),
                DriverId = Guid.Parse(response.DriverId),
                Origin = response.Origin,
                Destination = response.Destination,
                DepartureTime = DateTime.Parse(response.DepartureTime),
                AvailableSeats = response.AvailableSeats,
                PricePerSeat = (decimal)response.PricePerSeat,
                IsAvailable = response.IsAvailable
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("Ride {RideId} not found", rideId);
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // TODO: Remove mock data after Ride Service is ready
            _logger.LogWarning("Ride Service unavailable, returning mock data for {RideId}", rideId);
            return new RideInfoDto
            {
                RideId = rideId,
                DriverId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Origin = "Belgrade",
                Destination = "Novi Sad",
                DepartureTime = DateTime.UtcNow.AddDays(1),
                AvailableSeats = 4,
                PricePerSeat = 500m,
                IsAvailable = true
            };
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while getting ride info for {RideId}", rideId);
            throw;
        }
    }

    public async Task<bool> CheckAvailabilityAsync(
        Guid rideId, 
        int seatsRequested, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CheckAvailabilityRequest
            {
                RideId = rideId.ToString(),
                SeatsRequested = seatsRequested
            };

            var response = await _client.CheckAvailabilityAsync(request, cancellationToken: cancellationToken);
            return response.IsAvailable;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // TODO: Remove mock after Ride Service is ready
            _logger.LogWarning("Ride Service unavailable, returning mock availability for {RideId}", rideId);
            return true; // Always available in dev mode
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while checking availability for ride {RideId}", rideId);
            return false;
        }
    }

    public async Task<bool> ReserveSeatsAsync(
        Guid rideId, 
        int seatsCount, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ReserveSeatsRequest
            {
                RideId = rideId.ToString(),
                SeatsCount = seatsCount
            };

            var response = await _client.ReserveSeatsAsync(request, cancellationToken: cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogWarning(
                    "Failed to reserve {SeatsCount} seats for ride {RideId}: {Message}", 
                    seatsCount, rideId, response.Message);
            }

            return response.Success;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // TODO: Remove mock after Ride Service is ready
            _logger.LogWarning("Ride Service unavailable, mocking seat reservation for {RideId}", rideId);
            return true; // Always succeed in dev mode
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while reserving seats for ride {RideId}", rideId);
            return false;
        }
    }

    public async Task<bool> ReleaseSeatsAsync(
        Guid rideId, 
        int seatsCount, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ReleaseSeatsRequest
            {
                RideId = rideId.ToString(),
                SeatsCount = seatsCount
            };

            var response = await _client.ReleaseSeatsAsync(request, cancellationToken: cancellationToken);
            
            if (!response.Success)
            {
                _logger.LogWarning(
                    "Failed to release {SeatsCount} seats for ride {RideId}: {Message}", 
                    seatsCount, rideId, response.Message);
            }

            return response.Success;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            // TODO: Remove mock after Ride Service is ready
            _logger.LogWarning("Ride Service unavailable, mocking seat release for {RideId}", rideId);
            return true; // Always succeed in dev mode
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while releasing seats for ride {RideId}", rideId);
            return false;
        }
    }
}
