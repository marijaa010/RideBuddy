using SharedKernel;

namespace Booking.Domain.Events;

/// <summary>
/// Event emitted when a booking is confirmed.
/// </summary>
public class BookingConfirmedEvent : DomainEvent
{
    public Guid BookingId { get; }
    public Guid RideId { get; }
    public Guid PassengerId { get; }
    public int SeatsBooked { get; }
    public decimal TotalPrice { get; }
    public DateTime ConfirmedAt { get; }

    public BookingConfirmedEvent(
        Guid bookingId,
        Guid rideId,
        Guid passengerId,
        int seatsBooked,
        decimal totalPrice,
        DateTime confirmedAt)
    {
        BookingId = bookingId;
        RideId = rideId;
        PassengerId = passengerId;
        SeatsBooked = seatsBooked;
        TotalPrice = totalPrice;
        ConfirmedAt = confirmedAt;
    }
}
