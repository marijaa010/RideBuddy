using MediatR;
using Ride.Application.DTOs;

namespace Ride.Application.Queries.GetRideById;

public record GetRideByIdQuery : IRequest<RideDto?>
{
    public Guid RideId { get; init; }
}
