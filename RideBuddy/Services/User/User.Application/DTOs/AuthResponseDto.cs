namespace User.Application.DTOs;

/// <summary>
/// Authentication response containing the access token and user info.
/// </summary>
public record AuthResponseDto
{
    public string AccessToken { get; init; } = null!;
    public UserDto User { get; init; } = null!;
}
