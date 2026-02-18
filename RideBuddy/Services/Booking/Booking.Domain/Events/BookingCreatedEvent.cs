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
    public Guid DriverId { get; }
    public int SeatsBooked { get; }
    public decimal TotalPrice { get; }
    public string Currency { get; }
    public bool IsAutoConfirmed { get; }

    public BookingCreatedEvent(
        Guid bookingId,
        Guid rideId,
        Guid passengerId,
        Guid driverId,
        int seatsBooked,
        decimal totalPrice,
        string currency,
        bool isAutoConfirmed)
    {
        BookingId = bookingId;
        RideId = rideId;
        PassengerId = passengerId;
        DriverId = driverId;
        SeatsBooked = seatsBooked;
        TotalPrice = totalPrice;
        Currency = currency;
        IsAutoConfirmed = isAutoConfirmed;
    }
}
