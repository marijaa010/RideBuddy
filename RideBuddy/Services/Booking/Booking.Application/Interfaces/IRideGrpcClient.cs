using Booking.Application.DTOs;

namespace Booking.Application.Interfaces;

/// <summary>
/// Interface for gRPC communication with Ride Service.
/// </summary>
public interface IRideGrpcClient
{
    /// <summary>
    /// Checks ride availability and returns ride information.
    /// </summary>
    Task<RideInfoDto?> GetRideInfo(Guid rideId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there are enough available seats.
    /// </summary>
    Task<bool> CheckAvailability(Guid rideId, int seatsRequested, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves seats on a ride.
    /// </summary>
    Task<bool> ReserveSeats(Guid rideId, int seatsCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases previously reserved seats (compensation action).
    /// </summary>
    Task<bool> ReleaseSeats(Guid rideId, int seatsCount, CancellationToken cancellationToken = default);
}
