using MediatR;
using Ride.Application.Common;

namespace Ride.Application.Commands.CompleteRide;

public record CompleteRideCommand : IRequest<Result>
{
    public Guid RideId { get; init; }
    public Guid DriverId { get; init; }
}
