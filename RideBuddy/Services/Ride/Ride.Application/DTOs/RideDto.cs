using Ride.Domain.Enums;

namespace Ride.Application.DTOs;

/// <summary>
/// Data transfer object for ride display.
/// </summary>
public record RideDto
{
    public Guid Id { get; init; }
    public Guid DriverId { get; init; }
    public string OriginName { get; init; } = string.Empty;
    public double OriginLatitude { get; init; }
    public double OriginLongitude { get; init; }
    public string DestinationName { get; init; } = string.Empty;
    public double DestinationLatitude { get; init; }
    public double DestinationLongitude { get; init; }
    public DateTime DepartureTime { get; init; }
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }
    public decimal PricePerSeat { get; init; }
    public string Currency { get; init; } = "RSD";
    public RideStatus Status { get; init; }
    public bool AutoConfirmBookings { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
}
