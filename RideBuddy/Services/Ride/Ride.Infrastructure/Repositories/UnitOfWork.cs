using Microsoft.EntityFrameworkCore.Storage;
using Ride.Domain.Interfaces;
using Ride.Infrastructure.Persistence;

namespace Ride.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation ensuring transactional consistency.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly RideDbContext _context;
    private IDbContextTransaction? _transaction;
    private IRideRepository? _rides;
    private bool _disposed;

    public UnitOfWork(RideDbContext context)
    {
        _context = context;
    }

    public IRideRepository Rides => _rides ??= new RideRepository(_context);

    public async Task<int> SaveChanges(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransaction(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransaction(CancellationToken cancellationToken = default)
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

    public async Task RollbackTransaction(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;

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
