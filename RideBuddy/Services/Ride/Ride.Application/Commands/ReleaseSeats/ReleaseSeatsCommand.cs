using MediatR;
using Ride.Application.Common;

namespace Ride.Application.Commands.ReleaseSeats;

public record ReleaseSeatsCommand : IRequest<Result>
{
    public Guid RideId { get; init; }
    public int SeatsCount { get; init; }
}
