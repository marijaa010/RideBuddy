using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using User.Application.Interfaces;
using User.Domain.Entities;

namespace User.Infrastructure.Auth;

/// <summary>
/// JWT token generator.
/// Uses SymmetricSecurityKey with HMAC-SHA256.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a JWT token for authenticated user with embedded claims (user ID, email, roles).
    /// Token is signed with HMAC-SHA256 using secret key from environment variable.
    /// Called after successful login or registration.
    /// </summary>
    /// <param name="user">User entity to create token for</param>
    /// <param name="roles">User roles to embed in token claims</param>
    /// <returns>JWT token string that can be used for authentication</returns>
    public string GenerateToken(UserEntity user, IList<string> roles)
    {
        var signingCredentials = GetSigningCredentials();
        var claims = GetClaims(user, roles);
        var token = GenerateSecurityToken(signingCredentials, claims);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates signing credentials using secret key from environment variable (secure) or appsettings (fallback).
    /// Uses HMAC-SHA256 algorithm for token signature.
    /// </summary>
    /// <returns>SigningCredentials for JWT token generation</returns>
    /// <exception cref="InvalidOperationException">Thrown if secret key is not configured</exception>
    private SigningCredentials GetSigningCredentials()
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
            ?? _configuration.GetValue<string>("JwtSettings:secretKey");

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT Secret Key is not configured. Set JWT_SECRET_KEY environment variable.");
        }

        var key = Encoding.UTF8.GetBytes(secretKey);
        var secret = new SymmetricSecurityKey(key);

        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Builds list of claims to embed in JWT token.
    /// Includes user ID (multiple formats for compatibility), email, name, and roles.
    /// Claims are used for authorization and user identification in protected endpoints.
    /// </summary>
    /// <param name="user">User entity to extract claims from</param>
    /// <param name="roles">User roles to add as role claims</param>
    /// <returns>List of claims for JWT token</returns>
    private static List<Claim> GetClaims(UserEntity user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    /// <summary>
    /// Creates JwtSecurityToken with issuer, audience, expiration, and signature.
    /// Token expiration is read from appsettings (typically 60 minutes).
    /// </summary>
    /// <param name="signingCredentials">Credentials for signing the token</param>
    /// <param name="claims">Claims to embed in token payload</param>
    /// <returns>JwtSecurityToken ready to be serialized to string</returns>
    private JwtSecurityToken GenerateSecurityToken(
        SigningCredentials signingCredentials,
        IEnumerable<Claim> claims)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");

        var token = new JwtSecurityToken(
            issuer: jwtSettings.GetSection("validIssuer").Value,
            audience: jwtSettings.GetSection("validAudience").Value,
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                Convert.ToDouble(jwtSettings.GetSection("expires").Value)),
            signingCredentials: signingCredentials);

        return token;
    }
}
