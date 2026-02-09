using Ride.Domain.Entities;
using Ride.Domain.Enums;

namespace Ride.Domain.Interfaces;

/// <summary>
/// Repository interface for accessing rides.
/// </summary>
public interface IRideRepository
{
    Task<RideEntity?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RideEntity>> GetByDriverId(
        Guid driverId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RideEntity>> Search(
        string? origin, string? destination, DateTime? date,
        int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task Add(RideEntity ride, CancellationToken cancellationToken = default);

    Task Update(RideEntity ride, CancellationToken cancellationToken = default);
}
