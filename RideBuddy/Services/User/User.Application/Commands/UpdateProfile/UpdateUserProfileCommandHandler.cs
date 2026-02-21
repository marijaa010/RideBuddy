using MediatR;
using Microsoft.Extensions.Logging;
using User.Application.Common;
using User.Application.DTOs;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Domain.Interfaces;

namespace User.Application.Commands.UpdateProfile;

/// <summary>
/// Handler for updating a user's profile.
/// </summary>
public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating profile for user {UserId}", request.UserId);

        var user = await _unitOfWork.Users.GetById(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserDto>($"User with ID '{request.UserId}' not found.");
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);

        await _unitOfWork.Users.Update(user, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        await _eventPublisher.PublishMany(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        _logger.LogInformation("Profile updated for user {UserId}", request.UserId);

        return Result.Success(MapToDto(user));
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
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
