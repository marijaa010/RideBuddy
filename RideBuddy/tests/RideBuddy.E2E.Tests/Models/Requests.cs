namespace RideBuddy.E2E.Tests.Models;

public record RegisterRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Role { get; init; } = "Passenger";
}

public record LoginRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}

public record CreateRideRequest
{
    public string OriginName { get; init; } = string.Empty;
    public double OriginLatitude { get; init; }
    public double OriginLongitude { get; init; }
    public string DestinationName { get; init; } = string.Empty;
    public double DestinationLatitude { get; init; }
    public double DestinationLongitude { get; init; }
    public DateTime DepartureTime { get; init; }
    public int AvailableSeats { get; init; }
    public decimal PricePerSeat { get; init; }
    public string Currency { get; init; } = "RSD";
    public bool AutoConfirmBookings { get; init; } = true;
}

public record CreateBookingRequest
{
    public Guid RideId { get; init; }
    public int SeatsToBook { get; init; } = 1;
}

public record CancelBookingRequest
{
    public string? Reason { get; init; }
}

public record RejectBookingRequest
{
    public string? Reason { get; init; }
}
