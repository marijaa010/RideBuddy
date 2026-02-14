using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Protos;

namespace Notification.Infrastructure.Grpc;

public class UserGrpcClient : IUserGrpcClient
{
    private readonly UserGrpc.UserGrpcClient _client;
    private readonly ILogger<UserGrpcClient> _logger;

    public UserGrpcClient(UserGrpc.UserGrpcClient client, ILogger<UserGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<UserInfoDto?> GetUserInfo(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetUserInfoAsync(
                new GetUserInfoRequest { UserId = userId.ToString() },
                cancellationToken: ct);

            return new UserInfoDto
            {
                UserId = Guid.Parse(response.UserId),
                Email = response.Email,
                FirstName = response.FirstName,
                LastName = response.LastName
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user info for {UserId}", userId);
            return null;
        }
    }
}
