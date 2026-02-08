using MediatR;
using User.Application.Common;
using User.Application.DTOs;

namespace User.Application.Commands.RegisterUser;

/// <summary>
/// Command for registering a new user.
/// </summary>
public record RegisterUserCommand : IRequest<Result<AuthResponseDto>>
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Role { get; init; } = null!;
}
