using MediatR;
using Ride.Application.DTOs;

namespace Ride.Application.Queries.GetRidesByDriver;

public record GetRidesByDriverQuery : IRequest<IReadOnlyList<RideDto>>
{
    public Guid DriverId { get; init; }
}
