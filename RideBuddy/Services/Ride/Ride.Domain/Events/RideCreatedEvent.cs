using SharedKernel;

namespace Ride.Domain.Events;

/// <summary>
/// Event emitted when a new ride is created.
/// </summary>
public class RideCreatedEvent : DomainEvent
{
    public Guid RideId { get; }
    public Guid DriverId { get; }
    public string Origin { get; }
    public string Destination { get; }
    public DateTime DepartureTime { get; }
    public int AvailableSeats { get; }
    public decimal PricePerSeat { get; }
    public string Currency { get; }

    public RideCreatedEvent(
        Guid rideId, Guid driverId, string origin, string destination,
        DateTime departureTime, int availableSeats, decimal pricePerSeat, string currency)
    {
        RideId = rideId;
        DriverId = driverId;
        Origin = origin;
        Destination = destination;
        DepartureTime = departureTime;
        AvailableSeats = availableSeats;
        PricePerSeat = pricePerSeat;
        Currency = currency;
    }
}
