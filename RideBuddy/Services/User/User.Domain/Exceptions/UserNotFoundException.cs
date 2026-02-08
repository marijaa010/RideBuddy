namespace User.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user is not found.
/// </summary>
public class UserNotFoundException : UserDomainException
{
    public UserNotFoundException(Guid userId)
        : base($"User with ID '{userId}' was not found.") { }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.") { }
}
