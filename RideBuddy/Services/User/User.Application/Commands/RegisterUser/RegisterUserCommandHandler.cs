using MediatR;
using Microsoft.Extensions.Logging;
using User.Application.Common;
using User.Application.DTOs;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Domain.Enums;
using User.Domain.Interfaces;

namespace User.Application.Commands.RegisterUser;

/// <summary>
/// Handler for registering a new user.
/// Coordinates between Identity (Infrastructure) and the Domain entity.
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<AuthResponseDto>>
{
    private readonly IAuthenticationService _authService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IAuthenticationService authService,
        IJwtTokenGenerator jwtGenerator,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _authService = authService;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user with email {Email}", request.Email);

        // Step 1: Check if email already exists
        var exists = await _unitOfWork.Users.ExistsByEmail(request.Email, cancellationToken);
        if (exists)
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", request.Email);
            return Result.Failure<AuthResponseDto>("A user with this email already exists.");
        }

        // Step 2: Parse role
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return Result.Failure<AuthResponseDto>($"Invalid role '{request.Role}'. Must be Driver or Passenger.");
        }

        try
        {
            // Step 3: Create user in Identity (handles password hashing)
            var userId = await _authService.CreateUser(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Role,
                cancellationToken);

            // Step 4: Create domain entity
            var user = UserEntity.Register(
                userId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                role);

            // Step 5: Persist domain entity
            await _unitOfWork.Users.Add(user, cancellationToken);
            await _unitOfWork.SaveChanges(cancellationToken);

            // Step 6: Publish domain events
            await _eventPublisher.PublishMany(user.DomainEvents, cancellationToken);
            user.ClearDomainEvents();

            // Step 7: Generate JWT token
            var roles = await _authService.GetUserRoles(userId, cancellationToken);
            var token = _jwtGenerator.GenerateToken(user, roles);

            _logger.LogInformation("User {UserId} registered successfully", userId);

            return Result.Success(new AuthResponseDto
            {
                AccessToken = token,
                User = MapToDto(user)
            });
        }
        catch (Domain.Exceptions.UserDomainException ex)
        {
            // Domain validation errors (like password requirements) - return to user
            _logger.LogWarning("Registration validation failed for {Email}: {Error}", request.Email, ex.Message);
            return Result.Failure<AuthResponseDto>(ex.Message);
        }
        catch (Exception ex)
        {
            // Unexpected errors - log but don't expose details
            _logger.LogError(ex, "Unexpected error registering user with email {Email}", request.Email);
            return Result.Failure<AuthResponseDto>("An unexpected error occurred during registration.");
        }
    }

    private static UserDto MapToDto(UserEntity user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber.Value,
            Role = user.Role.ToString(),
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
