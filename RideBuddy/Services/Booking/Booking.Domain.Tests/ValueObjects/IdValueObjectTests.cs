using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;
using FluentAssertions;

namespace Booking.Domain.Tests.ValueObjects;

public class BookingIdTests
{
    [Fact]
    public void Create_WithValidGuid_ReturnsInstance()
    {
        var guid = Guid.NewGuid();

        var id = BookingId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsException()
    {
        var act = () => BookingId.Create(Guid.Empty);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("BookingId cannot be empty.");
    }

    [Fact]
    public void CreateNew_ReturnsNonEmptyId()
    {
        var id = BookingId.CreateNew();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        var a = BookingId.Create(guid);
        var b = BookingId.Create(guid);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentGuids_AreNotEqual()
    {
        var a = BookingId.CreateNew();
        var b = BookingId.CreateNew();

        a.Should().NotBe(b);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        var id = BookingId.Create(guid);

        Guid result = id;

        result.Should().Be(guid);
    }
}

public class RideIdTests
{
    [Fact]
    public void Create_WithValidGuid_ReturnsInstance()
    {
        var guid = Guid.NewGuid();

        var id = RideId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsException()
    {
        var act = () => RideId.Create(Guid.Empty);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("RideId cannot be empty.");
    }

    [Fact]
    public void CreateNew_ReturnsNonEmptyId()
    {
        var id = RideId.CreateNew();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        (RideId.Create(guid) == RideId.Create(guid)).Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        Guid result = RideId.Create(guid);

        result.Should().Be(guid);
    }
}

public class PassengerIdTests
{
    [Fact]
    public void Create_WithValidGuid_ReturnsInstance()
    {
        var guid = Guid.NewGuid();

        var id = PassengerId.Create(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsException()
    {
        var act = () => PassengerId.Create(Guid.Empty);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("PassengerId cannot be empty.");
    }

    [Fact]
    public void CreateNew_ReturnsNonEmptyId()
    {
        var id = PassengerId.CreateNew();

        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();

        (PassengerId.Create(guid) == PassengerId.Create(guid)).Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        Guid result = PassengerId.Create(guid);

        result.Should().Be(guid);
    }
}
