using SharedKernel;

namespace Booking.Domain.Events;

/// <summary>
/// Event emitted when a ride is completed and the booking is finalized.
/// </summary>
public class BookingCompletedEvent : DomainEvent
{
    public Guid BookingId { get; }
    public Guid RideId { get; }
    public Guid PassengerId { get; }
    public DateTime CompletedAt { get; }

    public BookingCompletedEvent(
        Guid bookingId,
        Guid rideId,
        Guid passengerId,
        DateTime completedAt)
    {
        BookingId = bookingId;
        RideId = rideId;
        PassengerId = passengerId;
        CompletedAt = completedAt;
    }
}
