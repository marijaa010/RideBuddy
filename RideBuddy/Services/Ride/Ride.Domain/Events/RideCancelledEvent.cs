using SharedKernel;

namespace Ride.Domain.Events;

/// <summary>
/// Event emitted when a ride is cancelled by the driver.
/// </summary>
public class RideCancelledEvent : DomainEvent
{
    public Guid RideId { get; }
    public Guid DriverId { get; }
    public string Reason { get; }
    public DateTime CancelledAt { get; }

    public RideCancelledEvent(Guid rideId, Guid driverId, string reason, DateTime cancelledAt)
    {
        RideId = rideId;
        DriverId = driverId;
        Reason = reason;
        CancelledAt = cancelledAt;
    }
}
