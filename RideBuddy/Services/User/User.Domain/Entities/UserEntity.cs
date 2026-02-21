using SharedKernel;
using User.Domain.Enums;
using User.Domain.Events;
using User.Domain.Exceptions;
using User.Domain.ValueObjects;

namespace User.Domain.Entities;

/// <summary>
/// Aggregate Root representing a user in the system.
/// This is the main entity of the User bounded context.
/// </summary>
public class UserEntity : AggregateRoot
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// User's first name.
    /// </summary>
    public string FirstName { get; private set; } = null!;

    /// <summary>
    /// User's last name.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// User's phone number.
    /// </summary>
    public PhoneNumber PhoneNumber { get; private set; } = null!;

    /// <summary>
    /// User's role in the system (Driver or Passenger).
    /// Drivers can also book rides as passengers.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Timestamp when the user registered.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when the profile was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core / mapping
    private UserEntity() { }

    /// <summary>
    /// Factory method to register a new user.
    /// </summary>
    public static UserEntity Register(
        Guid id,
        string email,
        string firstName,
        string lastName,
        string phoneNumber,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new UserDomainException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new UserDomainException("Last name cannot be empty.");

        var user = new UserEntity
        {
            Id = id,
            Email = Email.Create(email),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = PhoneNumber.Create(phoneNumber),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        user.AddDomainEvent(new UserRegisteredEvent(
            user.Id,
            email,
            firstName,
            lastName,
            role.ToString(),
            user.CreatedAt));

        return user;
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public void UpdateProfile(string firstName, string lastName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new UserDomainException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new UserDomainException("Last name cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = PhoneNumber.Create(phoneNumber);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new UserProfileUpdatedEvent(Id, UpdatedAt.Value));
    }
}
