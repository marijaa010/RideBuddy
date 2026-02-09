using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;
using FluentAssertions;
using System.Diagnostics.Metrics;
using System.Threading;

namespace Booking.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ReturnsMoneyInstance()
    {
        var money = Money.Create(100.50m, "RSD");

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("RSD");
    }

    [Fact]
    public void Create_WithZeroAmount_Succeeds()
    {
        var money = Money.Create(0, "EUR");

        money.Amount.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_OmittingCurrency_ThrowsException(string? currency)
    {
        var act = () => Money.Create(50m, currency!);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("Currency must be specified.");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("EURO")]
    [InlineData("123")]
    public void Create_WithInvalidCurrencyFormat_ThrowsException(string currency)
    {
        var act = () => Money.Create(50m, currency);
        act.Should().Throw<BookingDomainException>()
            .WithMessage("Currency must be a three-letter alphabetic code (ISO 4217).");
    }

    [Fact]
    public void Create_NormalizesCurrencyToUpperCase()
    {
        var money = Money.Create(10m, "eur");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsException()
    {
        var act = () => Money.Create(-1m, "RSD");

        act.Should().Throw<BookingDomainException>()
            .WithMessage("Amount cannot be negative.");
    }

    [Fact]
    public void Addition_SameCurrency_ReturnsSummedAmount()
    {
        var a = Money.Create(100m, "RSD");
        var b = Money.Create(250m, "RSD");

        var result = a + b;

        result.Amount.Should().Be(350m);
        result.Currency.Should().Be("RSD");
    }

    [Fact]
    public void Addition_DifferentCurrencies_ThrowsException()
    {
        var rsd = Money.Create(100m, "RSD");
        var eur = Money.Create(50m, "EUR");

        var act = () => rsd + eur;

        act.Should().Throw<BookingDomainException>()
            .WithMessage("Cannot add amounts in different currencies.");
    }

    [Fact]
    public void Multiplication_ReturnsMultipliedAmount()
    {
        var pricePerSeat = Money.Create(500m, "RSD");

        var total = pricePerSeat * 3;

        total.Amount.Should().Be(1500m);
        total.Currency.Should().Be("RSD");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = Money.Create(100m, "RSD");
        var b = Money.Create(100m, "RSD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentAmounts_AreNotEqual()
    {
        var a = Money.Create(100m, "RSD");
        var b = Money.Create(200m, "RSD");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrencies_AreNotEqual()
    {
        var a = Money.Create(100m, "RSD");
        var b = Money.Create(100m, "EUR");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var money = Money.Create(1500m, "RSD");

        money.ToString().Should().Contain("1,500.00").And.Contain("RSD");
    }
}
