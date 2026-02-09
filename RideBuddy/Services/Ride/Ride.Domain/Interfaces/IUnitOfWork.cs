namespace Ride.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRideRepository Rides { get; }
    Task<int> SaveChanges(CancellationToken cancellationToken = default);
    Task BeginTransaction(CancellationToken cancellationToken = default);
    Task CommitTransaction(CancellationToken cancellationToken = default);
    Task RollbackTransaction(CancellationToken cancellationToken = default);
}
