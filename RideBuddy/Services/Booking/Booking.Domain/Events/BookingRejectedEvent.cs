using Booking.Domain.Common;

namespace Booking.Domain.Events;

/// <summary>
/// Event emitted when a booking is rejected by the driver.
/// </summary>
public class BookingRejectedEvent : DomainEvent
{
    public Guid BookingId { get; }
    public Guid RideId { get; }
    public Guid PassengerId { get; }
    public int SeatsReleased { get; }
    public string RejectionReason { get; }
    public DateTime RejectedAt { get; }

    public BookingRejectedEvent(
        Guid bookingId,
        Guid rideId,
        Guid passengerId,
        int seatsReleased,
        string rejectionReason,
        DateTime rejectedAt)
    {
        BookingId = bookingId;
        RideId = rideId;
        PassengerId = passengerId;
        SeatsReleased = seatsReleased;
        RejectionReason = rejectionReason;
        RejectedAt = rejectedAt;
    }
}
