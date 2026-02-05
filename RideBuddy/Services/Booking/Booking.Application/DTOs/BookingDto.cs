using Booking.Domain.Enums;

namespace Booking.Application.DTOs;

/// <summary>
/// Data transfer object for booking display.
/// </summary>
public record BookingDto
{
    public Guid Id { get; init; }
    public Guid RideId { get; init; }
    public Guid PassengerId { get; init; }
    public Guid DriverId { get; init; }
    public int SeatsBooked { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = "RSD";
    public BookingStatus Status { get; init; }
    public DateTime BookedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? CancellationReason { get; init; }
}
