namespace User.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface.
/// Ensures all changes are saved in a single transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repository for users.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChanges(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    Task BeginTransaction(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    Task CommitTransaction(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    Task RollbackTransaction(CancellationToken cancellationToken = default);
}
