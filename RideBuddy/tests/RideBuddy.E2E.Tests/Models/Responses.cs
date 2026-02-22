namespace RideBuddy.E2E.Tests.Models;

public record AuthResponse
{
    public string AccessToken { get; init; } = null!;
    public UserResponse User { get; init; } = null!;
}

public record UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Role { get; init; } = null!;
}

public record RideResponse
{
    public Guid Id { get; init; }
    public Guid DriverId { get; init; }
    public string? DriverName { get; init; }
    public string OriginName { get; init; } = string.Empty;
    public string DestinationName { get; init; } = string.Empty;
    public DateTime DepartureTime { get; init; }
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
    public decimal PricePerSeat { get; init; }
    public string Currency { get; init; } = "RSD";
    public int Status { get; init; }
    public bool AutoConfirmBookings { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}

public record BookingResponse
{
    public Guid Id { get; init; }
    public Guid RideId { get; init; }
    public Guid PassengerId { get; init; }
    public string? PassengerName { get; init; }
    public Guid DriverId { get; init; }
    public int SeatsBooked { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = "RSD";
    public int Status { get; init; }
    public DateTime BookedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? CancellationReason { get; init; }
}

public record NotificationResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int Type { get; init; }
    public Guid? BookingId { get; init; }
    public Guid? RideId { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UnreadCountResponse
{
    public int Count { get; init; }
}
