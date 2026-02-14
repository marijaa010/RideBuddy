using Notification.Application.DTOs;

namespace Notification.Application.Interfaces;

/// <summary>
/// Pushes real-time notifications to connected clients via SignalR.
/// </summary>
public interface IRealTimeNotifier
{
    Task SendToUser(Guid userId, NotificationDto notification, CancellationToken ct = default);
}
