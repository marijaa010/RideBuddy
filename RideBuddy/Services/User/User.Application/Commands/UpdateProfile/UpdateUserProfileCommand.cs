using MediatR;
using User.Application.Common;
using User.Application.DTOs;

namespace User.Application.Commands.UpdateProfile;

/// <summary>
/// Command for updating a user's profile.
/// </summary>
public record UpdateUserProfileCommand : IRequest<Result<UserDto>>
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
}
