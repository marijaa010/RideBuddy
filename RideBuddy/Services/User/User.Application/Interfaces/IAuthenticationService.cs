using User.Domain.Entities;

namespace User.Application.Interfaces;

/// <summary>
/// Authentication service interface for Identity operations.
/// Implemented in Infrastructure using ASP.NET Core Identity.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validates user credentials and returns the domain user if valid.
    /// </summary>
    Task<UserEntity?> ValidateUser(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user in Identity and returns the generated ID.
    /// </summary>
    Task<Guid> CreateUser(
        string email,
        string password,
        string firstName,
        string lastName,
        string phoneNumber,
        string role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles assigned to a user.
    /// </summary>
    Task<IList<string>> GetUserRoles(Guid userId, CancellationToken cancellationToken = default);
}
