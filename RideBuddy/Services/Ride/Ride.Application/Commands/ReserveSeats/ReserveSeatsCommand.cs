using MediatR;
using Ride.Application.Common;

namespace Ride.Application.Commands.ReserveSeats;

public record ReserveSeatsCommand : IRequest<Result>
{
    public Guid RideId { get; init; }
    public int SeatsCount { get; init; }
}
