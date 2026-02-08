using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using User.Domain.Entities;
using User.Domain.Enums;
using User.Domain.Interfaces;
using User.Infrastructure.Persistence;

namespace User.Infrastructure.Repositories;

/// <summary>
/// Repository implementation that maps between the domain UserEntity and Identity's ApplicationUser.
/// Uses UserManager for Identity operations.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<UserEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var appUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id.ToString(), cancellationToken);

        return appUser is null ? null : MapToDomain(appUser);
    }

    public async Task<UserEntity?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var appUser = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized.ToUpperInvariant(), cancellationToken);

        return appUser is null ? null : MapToDomain(appUser);
    }

    public async Task<bool> ExistsByEmail(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public Task Add(UserEntity user, CancellationToken cancellationToken = default)
    {
        // User is already created via Identity's UserManager in AuthenticationService.
        // Domain entity data is stored on the same ApplicationUser record.
        return Task.CompletedTask;
    }

    public async Task Update(UserEntity user, CancellationToken cancellationToken = default)
    {
        var appUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id.ToString(), cancellationToken);

        if (appUser is null) return;

        appUser.FirstName = user.FirstName;
        appUser.LastName = user.LastName;
        appUser.PhoneNumber = user.PhoneNumber.Value;
        appUser.IsEmailVerified = user.IsEmailVerified;
        appUser.UpdatedAt = user.UpdatedAt;
        appUser.Role = user.Role.ToString();

        _context.Users.Update(appUser);
    }

    /// <summary>
    /// Maps an Identity ApplicationUser to a domain UserEntity.
    /// </summary>
    private static UserEntity MapToDomain(ApplicationUser appUser)
    {
        Enum.TryParse<UserRole>(appUser.Role, true, out var role);

        // Use the factory to create the domain entity with the existing ID
        var user = UserEntity.Register(
            Guid.Parse(appUser.Id),
            appUser.Email!,
            appUser.FirstName,
            appUser.LastName,
            appUser.PhoneNumber ?? "",
            role);

        // Clear domain events since this is a read, not a new registration
        user.ClearDomainEvents();

        return user;
    }
}
