using Booking.Application.DTOs;

namespace Booking.Application.Interfaces;

/// <summary>
/// Interface for gRPC communication with User Service.
/// </summary>
public interface IUserGrpcClient
{
    /// <summary>
    /// Validates whether the user exists and is active.
    /// </summary>
    Task<UserInfoDto?> ValidateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user information.
    /// </summary>
    Task<UserInfoDto?> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default);
}
