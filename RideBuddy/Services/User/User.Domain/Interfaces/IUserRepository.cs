using User.Domain.Entities;

namespace User.Domain.Interfaces;

/// <summary>
/// Repository interface for accessing users.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<UserEntity?> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    Task<UserEntity?> GetByEmail(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email already exists.
    /// </summary>
    Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user.
    /// </summary>
    Task Add(UserEntity user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task Update(UserEntity user, CancellationToken cancellationToken = default);
}
