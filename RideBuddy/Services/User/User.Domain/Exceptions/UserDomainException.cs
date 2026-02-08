namespace User.Domain.Exceptions;

/// <summary>
/// General domain exception for the User bounded context.
/// </summary>
public class UserDomainException : Exception
{
    public UserDomainException(string message) : base(message) { }
    public UserDomainException(string message, Exception innerException) : base(message, innerException) { }
}
