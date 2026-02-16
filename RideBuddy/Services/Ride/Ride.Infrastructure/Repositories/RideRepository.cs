using Microsoft.EntityFrameworkCore;
using Ride.Domain.Entities;
using Ride.Domain.Enums;
using Ride.Domain.Interfaces;
using Ride.Infrastructure.Persistence;

namespace Ride.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RideEntity using Entity Framework Core.
/// </summary>
public class RideRepository : IRideRepository
{
    private readonly RideDbContext _context;

    public RideRepository(RideDbContext context)
    {
        _context = context;
    }

    public async Task<RideEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RideEntity>> GetByDriverId(
        Guid driverId, CancellationToken cancellationToken = default)
    {
        return await _context.Rides
            .Where(r => r.DriverId == driverId)
            .OrderByDescending(r => r.DepartureTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RideEntity>> Search(
        string? origin, string? destination, DateTime? date,
        int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Rides
            .Where(r => r.Status == RideStatus.Scheduled && r.DepartureTime > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(origin))
        {
            var originLower = origin.ToLower();
            query = query.Where(r => r.Origin.Name.ToLower().Contains(originLower));
        }

        if (!string.IsNullOrWhiteSpace(destination))
        {
            var destLower = destination.ToLower();
            query = query.Where(r => r.Destination.Name.ToLower().Contains(destLower));
        }

        if (date.HasValue)
        {
            // Ensure DateTime has UTC kind for PostgreSQL timestamp with time zone
            var dateStart = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
            var dateEnd = dateStart.AddDays(1);
            query = query.Where(r => r.DepartureTime >= dateStart && r.DepartureTime < dateEnd);
        }

        return await query
            .OrderBy(r => r.DepartureTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task Add(RideEntity ride, CancellationToken cancellationToken = default)
    {
        await _context.Rides.AddAsync(ride, cancellationToken);
    }

    public Task Update(RideEntity ride, CancellationToken cancellationToken = default)
    {
        _context.Rides.Update(ride);
        return Task.CompletedTask;
    }
}
