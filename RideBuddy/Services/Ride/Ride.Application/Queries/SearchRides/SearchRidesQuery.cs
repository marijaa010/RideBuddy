using MediatR;
using Ride.Application.DTOs;

namespace Ride.Application.Queries.SearchRides;

public record SearchRidesQuery : IRequest<IReadOnlyList<RideDto>>
{
    public string? Origin { get; init; }
    public string? Destination { get; init; }
    public DateTime? Date { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
