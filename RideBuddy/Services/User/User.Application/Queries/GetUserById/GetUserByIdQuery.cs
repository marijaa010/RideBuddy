using MediatR;
using User.Application.DTOs;

namespace User.Application.Queries.GetUserById;

/// <summary>
/// Query for getting a user by their ID.
/// </summary>
public record GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid UserId { get; init; }
}
