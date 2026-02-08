using SharedKernel;

namespace Booking.Domain.Events;

/// <summary>
/// Event emitted when a new booking is created.
/// </summary>
public class BookingCreatedEvent : DomainEvent
{
    public Guid BookingId { get; }
    public Guid RideId { get; }
    public Guid PassengerId { get; }
    public int SeatsBooked { get; }
    public decimal TotalPrice { get; }
    public string Currency { get; }

    public BookingCreatedEvent(
        Guid bookingId,
        Guid rideId,
        Guid passengerId,
        int seatsBooked,
        decimal totalPrice,
        string currency)
    {
        BookingId = bookingId;
        RideId = rideId;
        PassengerId = passengerId;
        SeatsBooked = seatsBooked;
        TotalPrice = totalPrice;
        Currency = currency;
    }
}
