using Booking.Domain.Interfaces;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Booking.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation ensuring transactional consistency.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly BookingDbContext _context;
    private IDbContextTransaction? _transaction;
    private IBookingRepository? _bookings;
    private bool _disposed;

    public UnitOfWork(BookingDbContext context)
    {
        _context = context;
    }

    public IBookingRepository Bookings => _bookings ??= new BookingRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Transaction has not been started.");

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }
}
