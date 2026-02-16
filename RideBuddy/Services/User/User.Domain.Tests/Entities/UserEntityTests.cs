using FluentAssertions;
using User.Domain.Entities;
using User.Domain.Enums;
using User.Domain.Events;
using User.Domain.Exceptions;

namespace User.Domain.Tests.Entities;

public class UserEntityTests
{
    private const string ValidEmail = "john@example.com";
    private const string ValidFirstName = "John";
    private const string ValidLastName = "Doe";
    private const string ValidPhone = "+381641234567";

    // ---- Register (Factory) ----

    [Fact]
    public void Register_WithValidData_ShouldCreateUser()
    {
        var id = Guid.NewGuid();

        var user = UserEntity.Register(id, ValidEmail, ValidFirstName, ValidLastName, ValidPhone, UserRole.Passenger);

        user.Id.Should().Be(id);
        user.Email.Value.Should().Be(ValidEmail);
        user.FirstName.Should().Be(ValidFirstName);
        user.LastName.Should().Be(ValidLastName);
        user.PhoneNumber.Value.Should().Be(ValidPhone);
        user.Role.Should().Be(UserRole.Passenger);
        user.IsEmailVerified.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        user.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldRaise_UserRegisteredEvent()
    {
        var user = UserEntity.Register(Guid.NewGuid(), ValidEmail, ValidFirstName, ValidLastName, ValidPhone, UserRole.Driver);

        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserRegisteredEvent>();

        var evt = (UserRegisteredEvent)user.DomainEvents.First();
        evt.UserId.Should().Be(user.Id);
        evt.Email.Should().Be(ValidEmail);
        evt.Role.Should().Be("Driver");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Register_WithEmptyFirstName_ShouldThrow(string? firstName)
    {
        var act = () => UserEntity.Register(Guid.NewGuid(), ValidEmail, firstName!, ValidLastName, ValidPhone, UserRole.Passenger);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*First name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Register_WithEmptyLastName_ShouldThrow(string? lastName)
    {
        var act = () => UserEntity.Register(Guid.NewGuid(), ValidEmail, ValidFirstName, lastName!, ValidPhone, UserRole.Passenger);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*Last name*");
    }

    [Fact]
    public void Register_WithInvalidEmail_ShouldThrow()
    {
        var act = () => UserEntity.Register(Guid.NewGuid(), "not-an-email", ValidFirstName, ValidLastName, ValidPhone, UserRole.Passenger);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*valid email*");
    }

    // ---- UpdateProfile ----

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateFields()
    {
        var user = CreateValidUser();

        user.UpdateProfile("Jane", "Smith", "+381659876543");

        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.PhoneNumber.Value.Should().Be("+381659876543");
        user.UpdatedAt.Should().NotBeNull();
        user.Version.Should().Be(1);
    }

    [Fact]
    public void UpdateProfile_ShouldRaise_UserProfileUpdatedEvent()
    {
        var user = CreateValidUser();
        user.ClearDomainEvents();

        user.UpdateProfile("Jane", "Smith", "+381659876543");

        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserProfileUpdatedEvent>();
    }

    [Fact]
    public void UpdateProfile_WithEmptyFirstName_ShouldThrow()
    {
        var user = CreateValidUser();

        var act = () => user.UpdateProfile("", ValidLastName, ValidPhone);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*First name*");
    }

    // ---- VerifyEmail ----

    [Fact]
    public void VerifyEmail_WhenNotVerified_ShouldSetVerified()
    {
        var user = CreateValidUser();

        user.VerifyEmail();

        user.IsEmailVerified.Should().BeTrue();
        user.Version.Should().Be(1);
    }

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_ShouldThrow()
    {
        var user = CreateValidUser();
        user.VerifyEmail();

        var act = () => user.VerifyEmail();

        act.Should().Throw<UserDomainException>()
            .WithMessage("*already verified*");
    }

    // ---- ChangeRole ----

    [Fact]
    public void ChangeRole_ToDifferentRole_ShouldUpdate()
    {
        var user = CreateValidUser();

        user.ChangeRole(UserRole.Driver);

        user.Role.Should().Be(UserRole.Driver);
        user.Version.Should().Be(1);
    }

    [Fact]
    public void ChangeRole_ToSameRole_ShouldThrow()
    {
        var user = CreateValidUser();

        var act = () => user.ChangeRole(UserRole.Passenger);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*already has*");
    }

    // ---- ClearDomainEvents ----

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var user = CreateValidUser();
        user.DomainEvents.Should().NotBeEmpty();

        user.ClearDomainEvents();

        user.DomainEvents.Should().BeEmpty();
    }

    private static UserEntity CreateValidUser()
    {
        return UserEntity.Register(
            Guid.NewGuid(), ValidEmail, ValidFirstName, ValidLastName, ValidPhone, UserRole.Passenger);
    }
}
