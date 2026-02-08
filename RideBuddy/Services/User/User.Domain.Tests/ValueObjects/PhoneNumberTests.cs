using FluentAssertions;
using User.Domain.Exceptions;
using User.Domain.ValueObjects;

namespace User.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+381641234567")]
    [InlineData("0641234567")]
    [InlineData("+1 555 123 4567")]
    [InlineData("(011) 123-4567")]
    public void Create_WithValidPhone_ShouldSucceed(string phone)
    {
        var result = PhoneNumber.Create(phone);

        result.Value.Should().Be(phone.Trim());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyPhone_ShouldThrow(string? phone)
    {
        var act = () => PhoneNumber.Create(phone!);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*empty*");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("phone: 123")]
    public void Create_WithInvalidFormat_ShouldThrow(string phone)
    {
        var act = () => PhoneNumber.Create(phone);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*valid phone*");
    }

    [Fact]
    public void TwoPhones_WithSameValue_ShouldBeEqual()
    {
        var phone1 = PhoneNumber.Create("+381641234567");
        var phone2 = PhoneNumber.Create("+381641234567");

        phone1.Should().Be(phone2);
    }
}
