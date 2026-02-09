namespace Ride.Domain.Exceptions;

/// <summary>
/// Exception thrown when a ride is not found.
/// </summary>
public class RideNotFoundException : Exception
{
    public Guid RideId { get; }

    public RideNotFoundException(Guid rideId)
        : base($"Ride with ID '{rideId}' was not found.")
    {
        RideId = rideId;
    }
}
