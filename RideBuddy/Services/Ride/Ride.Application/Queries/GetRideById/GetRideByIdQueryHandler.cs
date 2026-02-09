using MediatR;
using Ride.Application.Commands.CreateRide;
using Ride.Application.DTOs;
using Ride.Domain.Interfaces;

namespace Ride.Application.Queries.GetRideById;

public class GetRideByIdQueryHandler : IRequestHandler<GetRideByIdQuery, RideDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRideByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RideDto?> Handle(GetRideByIdQuery request, CancellationToken cancellationToken)
    {
        var ride = await _unitOfWork.Rides.GetById(request.RideId, cancellationToken);
        return ride is null ? null : CreateRideCommandHandler.MapToDto(ride);
    }
}
