using MediatR;
using Ride.Application.Common;
using Ride.Application.DTOs;

namespace Ride.Application.Commands.CreateRide;

/// <summary>
/// Command for creating a new ride.
/// </summary>
public record CreateRideCommand : IRequest<Result<RideDto>>
{
    public Guid DriverId { get; init; }
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
