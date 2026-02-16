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
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
        {
            // Format validation errors for user display
            var errors = result.Errors.Select(e => e.Description).ToList();
            var errorMessage = errors.Count == 1 
                ? errors[0] 
                : "Password requirements: " + string.Join("; ", errors);
            
            _logger.LogWarning("Failed to create Identity user: {Errors}", errorMessage);
            throw new UserDomainException(errorMessage);
        }

        _logger.LogInformation("Created Identity user {UserId}", userId);

        // Assign role
        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (roleExists)
        {
            await _userManager.AddToRoleAsync(appUser, role);
            _logger.LogInformation("Assigned role {Role} to user {UserId}", role, userId);
        }

        return userId;
    }

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
