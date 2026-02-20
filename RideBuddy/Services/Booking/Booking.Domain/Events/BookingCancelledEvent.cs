using SharedKernel;

namespace Booking.Domain.Events;

/// <summary>
/// Event emitted when a booking is cancelled.
/// </summary>
public class BookingCancelledEvent : DomainEvent
{
    public Guid BookingId { get; }
    public Guid RideId { get; }
    public Guid PassengerId { get; }
    public Guid DriverId { get; }
    public int SeatsReleased { get; }
    public string CancellationReason { get; }
    public DateTime CancelledAt { get; }
    public bool CancelledByPassenger { get; }
    public DateTime DepartureTime { get; }

    public BookingCancelledEvent(
        Guid bookingId,
        Guid rideId,
        Guid passengerId,
        Guid driverId,
        int seatsReleased,
        string cancellationReason,
        DateTime cancelledAt,
        bool cancelledByPassenger,
        DateTime departureTime)
    {
        BookingId = bookingId;
        RideId = rideId;
        PassengerId = passengerId;
        DriverId = driverId;
        SeatsReleased = seatsReleased;
        CancellationReason = cancellationReason;
        CancelledAt = cancelledAt;
        CancelledByPassenger = cancelledByPassenger;
        DepartureTime = departureTime;
    }
}
