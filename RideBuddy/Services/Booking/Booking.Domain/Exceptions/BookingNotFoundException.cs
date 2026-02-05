namespace Booking.Domain.Exceptions;

/// <summary>
/// Exception thrown when a booking is not found.
/// </summary>
public class BookingNotFoundException : Exception
{
    public Guid BookingId { get; }

    public BookingNotFoundException(Guid bookingId) 
        : base($"Booking with ID '{bookingId}' was not found.")
    {
        BookingId = bookingId;
    }
}
