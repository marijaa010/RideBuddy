using SharedKernel;

namespace Ride.Domain.Events;

/// <summary>
/// Event emitted when previously reserved seats are released.
/// </summary>
public class SeatsReleasedEvent : DomainEvent
{
    public Guid RideId { get; }
    public int SeatsReleased { get; }
    public int RemainingSeats { get; }

    public SeatsReleasedEvent(Guid rideId, int seatsReleased, int remainingSeats)
    {
        RideId = rideId;
        SeatsReleased = seatsReleased;
        RemainingSeats = remainingSeats;
    }
}
