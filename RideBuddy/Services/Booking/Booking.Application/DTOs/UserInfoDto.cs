namespace Booking.Application.DTOs;

/// <summary>
/// DTO containing user information received via gRPC.
/// </summary>
public record UserInfoDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsValid { get; init; }
}
