using Notification.Domain.Enums;

namespace Notification.Application.DTOs;

public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public Guid? BookingId { get; init; }
    public Guid? RideId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UserInfoDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>
/// Mirrors the booking domain events for JSON deserialization.
/// </summary>
public record BookingEventDto
{
    public Guid BookingId { get; init; }
    public Guid RideId { get; init; }
    public Guid PassengerId { get; init; }
    public Guid DriverId { get; init; }
    public int SeatsBooked { get; init; }
    public int SeatsReleased { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string CancellationReason { get; init; } = string.Empty;
    public string RejectionReason { get; init; } = string.Empty;
    public DateTime ConfirmedAt { get; init; }
    public DateTime CancelledAt { get; init; }
    public DateTime RejectedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public bool IsAutoConfirmed { get; init; }
    public bool CancelledByPassenger { get; init; }
    public DateTime DepartureTime { get; init; }
}
