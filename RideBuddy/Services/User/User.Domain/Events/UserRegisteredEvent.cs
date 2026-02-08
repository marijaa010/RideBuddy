using SharedKernel;

namespace User.Domain.Events;

/// <summary>
/// Domain event raised when a new user registers.
/// </summary>
public class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Role { get; }
    public DateTime RegisteredAt { get; }

    public UserRegisteredEvent(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        string role,
        DateTime registeredAt)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        RegisteredAt = registeredAt;
    }
}
