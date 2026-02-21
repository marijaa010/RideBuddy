using MediatR;
using Microsoft.Extensions.Logging;
using User.Application.Common;
using User.Application.DTOs;
using User.Application.Interfaces;
using User.Domain.Entities;

namespace User.Application.Queries.LoginUser;

/// <summary>
/// Handler for authenticating a user.
/// </summary>
public class LoginUserQueryHandler : IRequestHandler<LoginUserQuery, Result<AuthResponseDto>>
{
    private readonly IAuthenticationService _authService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly ILogger<LoginUserQueryHandler> _logger;

    public LoginUserQueryHandler(
        IAuthenticationService authService,
        IJwtTokenGenerator jwtGenerator,
        ILogger<LoginUserQueryHandler> logger)
    {
        _authService = authService;
        _jwtGenerator = jwtGenerator;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        LoginUserQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for {Email}", request.Email);

        var user = await _authService.ValidateUser(request.Email, request.Password, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login failed for {Email}: invalid credentials", request.Email);
            return Result.Failure<AuthResponseDto>("Invalid email or password.");
        }

        var roles = await _authService.GetUserRoles(user.Id, cancellationToken);
        var token = _jwtGenerator.GenerateToken(user, roles);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return Result.Success(new AuthResponseDto
        {
            AccessToken = token,
            User = MapToDto(user)
        });
    }

    private static UserDto MapToDto(UserEntity user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber.Value,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
