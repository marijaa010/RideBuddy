using User.Application.DTOs;
using User.Domain.Entities;

namespace User.Application.Interfaces;

/// <summary>
/// JWT token generator interface.
/// Implemented in Infrastructure.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a JWT access token for the given user.
    /// </summary>
    string GenerateToken(UserEntity user, IList<string> roles);
}
