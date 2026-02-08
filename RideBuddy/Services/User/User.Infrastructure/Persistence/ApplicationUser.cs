using Microsoft.AspNetCore.Identity;

namespace User.Infrastructure.Persistence;

/// <summary>
/// ASP.NET Core Identity user entity.
/// Extends IdentityUser with additional profile fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
