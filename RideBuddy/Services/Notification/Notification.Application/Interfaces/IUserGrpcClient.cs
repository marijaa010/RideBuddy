using Notification.Application.DTOs;

namespace Notification.Application.Interfaces;

/// <summary>
/// gRPC client for fetching user info from User Service.
/// </summary>
public interface IUserGrpcClient
{
    Task<UserInfoDto?> GetUserInfo(Guid userId, CancellationToken ct = default);
}
