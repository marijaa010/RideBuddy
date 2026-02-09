using MediatR;
using Ride.Application.Common;

namespace Ride.Application.Commands.CancelRide;

public record CancelRideCommand : IRequest<Result>
{
    public Guid RideId { get; init; }
    public Guid DriverId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
