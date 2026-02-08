using FluentAssertions;
using User.Domain.Exceptions;
using User.Domain.ValueObjects;

namespace User.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("john@example.com")]
    [InlineData("user.name@domain.co")]
    [InlineData("test+tag@gmail.com")]
    public void Create_WithValidEmail_ShouldSucceed(string email)
    {
        var result = Email.Create(email);

        result.Value.Should().Be(email.Trim().ToLowerInvariant());
    }

    [Fact]
    public void Create_ShouldNormalizeToLowercase()
    {
        var result = Email.Create("John@Example.COM");

        result.Value.Should().Be("john@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ShouldThrow(string? email)
    {
        var act = () => Email.Create(email!);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*empty*");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@nodomain")]
    [InlineData("no@")]
    [InlineData("spaces in@email.com")]
    public void Create_WithInvalidFormat_ShouldThrow(string email)
    {
        var act = () => Email.Create(email);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*valid email*");
    }

    [Fact]
    public void TwoEmails_WithSameValue_ShouldBeEqual()
    {
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void TwoEmails_WithDifferentValues_ShouldNotBeEqual()
    {
        var email1 = Email.Create("a@example.com");
        var email2 = Email.Create("b@example.com");

        email1.Should().NotBe(email2);
    }
}
