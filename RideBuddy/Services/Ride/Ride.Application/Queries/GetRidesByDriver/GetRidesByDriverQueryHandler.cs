using MediatR;
using Ride.Application.Commands.CreateRide;
using Ride.Application.DTOs;
using Ride.Domain.Interfaces;

namespace Ride.Application.Queries.GetRidesByDriver;

public class GetRidesByDriverQueryHandler : IRequestHandler<GetRidesByDriverQuery, IReadOnlyList<RideDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRidesByDriverQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RideDto>> Handle(GetRidesByDriverQuery request, CancellationToken cancellationToken)
    {
        var rides = await _unitOfWork.Rides.GetByDriverId(request.DriverId, cancellationToken);
        return rides.Select(CreateRideCommandHandler.MapToDto).ToList();
    }
}
