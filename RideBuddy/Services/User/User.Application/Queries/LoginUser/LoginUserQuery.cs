using MediatR;
using User.Application.Common;
using User.Application.DTOs;

namespace User.Application.Queries.LoginUser;

/// <summary>
/// Query for authenticating a user and returning a JWT token.
/// </summary>
public record LoginUserQuery : IRequest<Result<AuthResponseDto>>
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}
