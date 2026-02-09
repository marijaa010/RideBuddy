using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ride.Application.DTOs;
using Ride.Application.Interfaces;
using Ride.Infrastructure.Protos;

namespace Ride.Infrastructure.Services;

/// <summary>
/// gRPC client for communication with User Service.
/// </summary>
public class UserGrpcClient : IUserGrpcClient
{
    private readonly UserGrpc.UserGrpcClient _client;
    private readonly ILogger<UserGrpcClient> _logger;

    public UserGrpcClient(UserGrpc.UserGrpcClient client, ILogger<UserGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<UserInfoDto?> ValidateUser(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ValidateUserRequest { UserId = userId.ToString() };
            var response = await _client.ValidateUserAsync(request, cancellationToken: cancellationToken);

            return new UserInfoDto
            {
                UserId = Guid.Parse(response.UserId),
                Email = response.Email,
                FirstName = response.FirstName,
                LastName = response.LastName,
                PhoneNumber = response.PhoneNumber,
                IsValid = response.IsValid
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return null;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while validating user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserInfoDto?> GetUserInfo(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetUserInfoRequest { UserId = userId.ToString() };
            var response = await _client.GetUserInfoAsync(request, cancellationToken: cancellationToken);

            return new UserInfoDto
            {
                UserId = Guid.Parse(response.UserId),
                Email = response.Email,
                FirstName = response.FirstName,
                LastName = response.LastName,
                PhoneNumber = response.PhoneNumber,
                IsValid = response.IsValid
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error while getting user info for {UserId}", userId);
            throw;
        }
    }
}
