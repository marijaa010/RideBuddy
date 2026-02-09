using MediatR;
using Ride.Application.Commands.CreateRide;
using Ride.Application.DTOs;
using Ride.Domain.Interfaces;

namespace Ride.Application.Queries.SearchRides;

public class SearchRidesQueryHandler : IRequestHandler<SearchRidesQuery, IReadOnlyList<RideDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchRidesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RideDto>> Handle(SearchRidesQuery request, CancellationToken cancellationToken)
    {
        var rides = await _unitOfWork.Rides.Search(
            request.Origin, request.Destination, request.Date,
            request.Page, request.PageSize,
            cancellationToken);

        return rides.Select(CreateRideCommandHandler.MapToDto).ToList();
    }
}
