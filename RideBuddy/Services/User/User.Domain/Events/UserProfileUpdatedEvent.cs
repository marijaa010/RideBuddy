using SharedKernel;

namespace User.Domain.Events;

/// <summary>
/// Domain event raised when a user updates their profile.
/// </summary>
public class UserProfileUpdatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public DateTime UpdatedAt { get; }

    public UserProfileUpdatedEvent(Guid userId, DateTime updatedAt)
    {
        UserId = userId;
        UpdatedAt = updatedAt;
    }
}
