using Ride.Domain.Exceptions;
using Ride.Domain.ValueObjects;

namespace Ride.Domain.Tests.ValueObjects;

public class LocationTests
{
    [Fact]
    public void Create_ValidData_CreatesLocation()
    {
        var location = Location.Create("Novi Sad", 45.2671, 19.8335);

        location.Name.Should().Be("Novi Sad");
        location.Latitude.Should().Be(45.2671);
        location.Longitude.Should().Be(19.8335);
    }

    [Theory]
    [InlineData("", 0, 0)]
    [InlineData(" ", 0, 0)]
    [InlineData(null, 0, 0)]
    public void Create_EmptyName_ThrowsException(string? name, double lat, double lng)
    {
        var act = () => Location.Create(name!, lat, lng);
        act.Should().Throw<RideDomainException>();
    }

    [Fact]
    public void Create_InvalidLatitude_ThrowsException()
    {
        var act = () => Location.Create("Test", 91, 0);
        act.Should().Throw<RideDomainException>().WithMessage("*Latitude*");
    }

    [Fact]
    public void Create_InvalidLongitude_ThrowsException()
    {
        var act = () => Location.Create("Test", 0, 181);
        act.Should().Throw<RideDomainException>().WithMessage("*Longitude*");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Location.Create("Beograd", 44.7866, 20.4489);
        var b = Location.Create("Beograd", 44.7866, 20.4489);

        a.Should().Be(b);
    }
}

public class MoneyTests
{
    [Fact]
    public void Create_ValidData_CreatesMoney()
    {
        var money = Money.Create(500m, "RSD");

        money.Amount.Should().Be(500m);
        money.Currency.Should().Be("RSD");
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsException()
    {
        var act = () => Money.Create(-1m, "RSD");
        act.Should().Throw<RideDomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_InvalidCurrency_ThrowsException()
    {
        var act = () => Money.Create(100m, "AB");
        act.Should().Throw<RideDomainException>().WithMessage("*three-letter*");
    }
}

public class SeatsCountTests
{
    [Fact]
    public void Create_ValidCount_CreatesSeatsCount()
    {
        var seats = SeatsCount.Create(4);
        seats.Value.Should().Be(4);
    }

    [Fact]
    public void Create_NegativeCount_ThrowsException()
    {
        var act = () => SeatsCount.Create(-1);
        act.Should().Throw<RideDomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_Zero_AllowsZero()
    {
        var seats = SeatsCount.Create(0);
        seats.Value.Should().Be(0);
    }
}

public class DriverIdTests
{
    [Fact]
    public void Create_ValidGuid_CreatesDriverId()
    {
        var id = Guid.NewGuid();
        var driverId = DriverId.Create(id);
        driverId.Value.Should().Be(id);
    }

    [Fact]
    public void Create_EmptyGuid_ThrowsException()
    {
        var act = () => DriverId.Create(Guid.Empty);
        act.Should().Throw<RideDomainException>();
    }
}
