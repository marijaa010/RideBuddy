using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Domain.Enums;
using User.Domain.Exceptions;
using User.Infrastructure.Persistence;

namespace User.Infrastructure.Auth;

/// <summary>
/// Authentication service implemented using ASP.NET Core Identity.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Validates user credentials (email and password) for login.
    /// Uses ASP.NET Core Identity's password hashing for verification.
    /// Maps ApplicationUser (Infrastructure) to UserEntity (Domain) on success.
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="password">Plain text password (hashed internally by Identity)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>UserEntity if credentials valid, null otherwise</returns>
    public async Task<UserEntity?> ValidateUser(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        Enum.TryParse<UserRole>(user.Role, true, out var role);

        var domainUser = UserEntity.Register(
            Guid.Parse(user.Id),
            user.Email!,
            user.FirstName,
            user.LastName,
            user.PhoneNumber ?? "",
            role);

        domainUser.ClearDomainEvents();
        return domainUser;
    }

    /// <summary>
    /// Creates new user account with ASP.NET Core Identity.
    /// Automatically hashes password using Identity's PBKDF2 algorithm.
    /// Creates user in AspNetUsers table and assigns role.
    /// </summary>
    /// <param name="email">User email (also used as username)</param>
    /// <param name="password">Plain text password (will be hashed)</param>
    /// <param name="firstName">User first name</param>
    /// <param name="lastName">User last name</param>
    /// <param name="phoneNumber">User phone number</param>
    /// <param name="role">User role (Driver or Passenger)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Newly created user ID</returns>
    /// <exception cref="UserDomainException">Thrown if user creation fails (e.g., duplicate email, weak password)</exception>
    public async Task<Guid> CreateUser(
        string email,
        string password,
        string firstName,
        string lastName,
        string phoneNumber,
        string role,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.NewGuid();

        var appUser = new ApplicationUser
        {
            Id = userId.ToString(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            var errorMessage = errors.Count == 1 
                ? errors[0] 
                : "Password requirements: " + string.Join("; ", errors);
            
            _logger.LogWarning("Failed to create Identity user: {Errors}", errorMessage);
            throw new UserDomainException(errorMessage);
        }

        _logger.LogInformation("Created Identity user {UserId}", userId);

        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (roleExists)
        {
            await _userManager.AddToRoleAsync(appUser, role);
            _logger.LogInformation("Assigned role {Role} to user {UserId}", role, userId);
        }

        return userId;
    }

    /// <summary>
    /// Retrieves all roles assigned to a user.
    /// Used during JWT token generation to embed role claims.
    /// </summary>
    /// <param name="userId">User ID to fetch roles for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of role names assigned to user (e.g., ["Driver"])</returns>
    public async Task<IList<string>> GetUserRoles(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }
}
