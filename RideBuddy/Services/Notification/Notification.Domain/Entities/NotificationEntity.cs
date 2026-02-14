namespace Notification.Domain.Entities;

using Notification.Domain.Enums;

/// <summary>
/// Represents an in-app notification stored in the database.
/// </summary>
public class NotificationEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid? RideId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private NotificationEntity() { }

    public static NotificationEntity Create(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        Guid? bookingId = null,
        Guid? rideId = null)
    {
        return new NotificationEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            BookingId = bookingId,
            RideId = rideId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
