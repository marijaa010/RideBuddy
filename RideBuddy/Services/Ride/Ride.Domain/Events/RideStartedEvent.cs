using SharedKernel;

namespace Ride.Domain.Events;

/// <summary>
/// Event emitted when a ride starts.
/// </summary>
public class RideStartedEvent : DomainEvent
{
    public Guid RideId { get; }
    public Guid DriverId { get; }
    public DateTime StartedAt { get; }

    public RideStartedEvent(Guid rideId, Guid driverId, DateTime startedAt)
    {
        RideId = rideId;
        DriverId = driverId;
        StartedAt = startedAt;
    }
}
