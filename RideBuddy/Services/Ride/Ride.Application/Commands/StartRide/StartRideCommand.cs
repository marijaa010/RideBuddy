using MediatR;
using Ride.Application.Common;

namespace Ride.Application.Commands.StartRide;

public record StartRideCommand : IRequest<Result>
{
    public Guid RideId { get; init; }
    public Guid DriverId { get; init; }
}
