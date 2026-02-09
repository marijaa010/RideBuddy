using SharedKernel;

namespace Ride.Domain.Events;

/// <summary>
/// Event emitted when a ride is completed.
/// </summary>
public class RideCompletedEvent : DomainEvent
{
    public Guid RideId { get; }
    public Guid DriverId { get; }
    public DateTime CompletedAt { get; }

    public RideCompletedEvent(Guid rideId, Guid driverId, DateTime completedAt)
    {
        RideId = rideId;
        DriverId = driverId;
        CompletedAt = completedAt;
    }
}
