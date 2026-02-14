using Notification.Domain.Entities;

namespace Notification.Application.Interfaces;

/// <summary>
/// Repository for in-app notifications.
/// </summary>
public interface INotificationRepository
{
    Task<NotificationEntity?> GetById(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationEntity>> GetByUserId(Guid userId, bool unreadOnly = false, CancellationToken ct = default);
    Task<int> GetUnreadCount(Guid userId, CancellationToken ct = default);
    Task Add(NotificationEntity notification, CancellationToken ct = default);
    Task Update(NotificationEntity notification, CancellationToken ct = default);
    Task MarkAllAsRead(Guid userId, CancellationToken ct = default);
}
