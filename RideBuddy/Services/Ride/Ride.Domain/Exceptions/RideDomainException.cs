namespace Ride.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated in the Ride domain.
/// </summary>
public class RideDomainException : Exception
{
    public RideDomainException(string message) : base(message) { }
    public RideDomainException(string message, Exception innerException) : base(message, innerException) { }
}
