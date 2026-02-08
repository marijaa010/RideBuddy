using FluentAssertions;
using User.Domain.Exceptions;
using User.Domain.ValueObjects;

namespace User.Domain.Tests.ValueObjects;

public class UserIdTests
{
    [Fact]
    public void Create_WithValidGuid_ShouldSucceed()
    {
        var guid = Guid.NewGuid();

        var userId = UserId.Create(guid);

        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithEmptyGuid_ShouldThrow()
    {
        var act = () => UserId.Create(Guid.Empty);

        act.Should().Throw<UserDomainException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void CreateNew_ShouldGenerateNonEmptyGuid()
    {
        var userId = UserId.CreateNew();

        userId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void TwoUserIds_WithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = UserId.Create(guid);
        var id2 = UserId.Create(guid);

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void TwoUserIds_WithDifferentGuids_ShouldNotBeEqual()
    {
        var id1 = UserId.CreateNew();
        var id2 = UserId.CreateNew();

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        var guid = Guid.NewGuid();
        var userId = UserId.Create(guid);

        Guid result = userId;

        result.Should().Be(guid);
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        var guid = Guid.NewGuid();
        var userId = UserId.Create(guid);

        userId.ToString().Should().Be(guid.ToString());
    }
}
