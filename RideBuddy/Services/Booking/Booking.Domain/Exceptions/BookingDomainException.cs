namespace Booking.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated in the domain.
/// </summary>
public class BookingDomainException : Exception
{
    public BookingDomainException(string message) : base(message)
    {
    }

    public BookingDomainException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
