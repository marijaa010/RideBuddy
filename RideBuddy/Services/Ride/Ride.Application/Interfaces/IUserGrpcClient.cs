using Ride.Application.DTOs;

namespace Ride.Application.Interfaces;

/// <summary>
/// Interface for gRPC communication with User Service.
/// </summary>
public interface IUserGrpcClient
{
    Task<UserInfoDto?> ValidateUser(Guid userId, CancellationToken cancellationToken = default);
    Task<UserInfoDto?> GetUserInfo(Guid userId, CancellationToken cancellationToken = default);
}
