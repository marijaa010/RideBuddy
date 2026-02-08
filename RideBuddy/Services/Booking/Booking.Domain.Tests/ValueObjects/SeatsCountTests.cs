using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;
using FluentAssertions;

namespace Booking.Domain.Tests.ValueObjects;

public class SeatsCountTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void Create_WithValidCount_ReturnsInstance(int count)
    {
        var seats = SeatsCount.Create(count);

        seats.Value.Should().Be(count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithZeroOrNegative_ThrowsException(int count)
    {
        var act = () => SeatsCount.Create(count);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("Number of seats must be greater than 0.");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = SeatsCount.Create(3);
        var b = SeatsCount.Create(3);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = SeatsCount.Create(2);
        var b = SeatsCount.Create(5);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        var seats = SeatsCount.Create(4);

        int value = seats;

        value.Should().Be(4);
    }

    [Fact]
    public void ToString_ReturnsStringValue()
    {
        var seats = SeatsCount.Create(3);

        seats.ToString().Should().Be("3");
    }
}
