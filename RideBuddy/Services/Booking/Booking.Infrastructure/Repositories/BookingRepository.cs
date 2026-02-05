using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Interfaces;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Booking.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BookingEntity using Entity Framework Core.
/// </summary>
public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BookingEntity>> GetByPassengerIdAsync(
        Guid passengerId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.PassengerId == passengerId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingEntity>> GetByRideIdAsync(
        Guid rideId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.RideId == rideId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingEntity>> GetByPassengerAndStatusAsync(
        Guid passengerId, 
        BookingStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => b.PassengerId == passengerId && b.Status == status)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsActiveBookingAsync(
        Guid passengerId, 
        Guid rideId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AnyAsync(b => 
                b.PassengerId == passengerId && 
                b.RideId == rideId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed),
                cancellationToken);
    }

    public async Task AddAsync(BookingEntity booking, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(booking, cancellationToken);
    }

    public Task UpdateAsync(BookingEntity booking, CancellationToken cancellationToken = default)
    {
        _context.Bookings.Update(booking);
        return Task.CompletedTask;
    }

    public async Task<int> GetTotalBookedSeatsForRideAsync(
        Guid rideId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(b => 
                b.RideId == rideId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .SumAsync(b => b.SeatsBooked, cancellationToken);
    }
}
