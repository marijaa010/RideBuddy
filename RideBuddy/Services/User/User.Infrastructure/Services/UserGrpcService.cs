using Grpc.Core;
using Microsoft.Extensions.Logging;
using User.Domain.Interfaces;
using User.Infrastructure.Protos;

namespace User.Infrastructure.Services;

/// <summary>
/// gRPC server implementation for the User service.
/// Handles requests from Booking and Ride services.
/// </summary>
public class UserGrpcService : UserGrpc.UserGrpcBase
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(IUserRepository repository, ILogger<UserGrpcService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public override async Task<UserInfoResponse> ValidateUser(
        ValidateUserRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ValidateUser called for {UserId}", request.UserId);

        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return new UserInfoResponse { IsValid = false };
        }

        var user = await _repository.GetById(userId, context.CancellationToken);

        if (user is null)
        {
            return new UserInfoResponse { IsValid = false };
        }

        return new UserInfoResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber.Value,
            IsValid = true
        };
    }

    public override async Task<UserInfoResponse> GetUserInfo(
        GetUserInfoRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetUserInfo called for {UserId}", request.UserId);

        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID."));
        }

        var user = await _repository.GetById(userId, context.CancellationToken);

        if (user is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found."));
        }

        return new UserInfoResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber.Value,
            IsValid = true
        };
    }
}
