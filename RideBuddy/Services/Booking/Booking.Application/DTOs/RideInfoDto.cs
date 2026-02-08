namespace Booking.Application.DTOs;

/// <summary>
/// DTO containing ride information received via gRPC.
/// </summary>
public record RideInfoDto
{
    public Guid RideId { get; init; }
    public Guid DriverId { get; init; }
    public string Origin { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public DateTime DepartureTime { get; init; }
    public int AvailableSeats { get; init; }
    public decimal PricePerSeat { get; init; }
    public string Currency { get; init; } = "RSD";
    public bool IsAvailable { get; init; }
}
