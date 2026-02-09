using SharedKernel;

namespace Ride.Domain.Events;

/// <summary>
/// Event emitted when seats are reserved on a ride.
/// </summary>
public class SeatsReservedEvent : DomainEvent
{
    public Guid RideId { get; }
    public int SeatsReserved { get; }
    public int RemainingSeats { get; }

    public SeatsReservedEvent(Guid rideId, int seatsReserved, int remainingSeats)
    {
        RideId = rideId;
        SeatsReserved = seatsReserved;
        RemainingSeats = remainingSeats;
    }
}
