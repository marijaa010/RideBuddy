using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;

namespace Notification.Infrastructure.RealTime;

/// <summary>
/// SignalR hub for real-time notifications.
/// Clients connect and join a group based on their user ID.
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client calls this after connecting to join their personal notification group.
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        _logger.LogInformation(
            "Connection {ConnectionId} joined group user-{UserId}",
            Context.ConnectionId, userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Pushes notifications to connected SignalR clients.
/// </summary>
public class SignalRNotifier : IRealTimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotifier> _logger;

    public SignalRNotifier(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUser(Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        var groupName = $"user-{userId}";

        await _hubContext.Clients.Group(groupName).SendAsync(
            "ReceiveNotification", notification, ct);

        _logger.LogInformation(
            "Pushed notification {NotificationId} to group {Group}",
            notification.Id, groupName);
    }
}
