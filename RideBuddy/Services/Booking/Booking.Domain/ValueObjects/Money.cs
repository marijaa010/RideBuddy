using Booking.Domain.Common;
using Booking.Domain.Exceptions;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Value Object representing a monetary amount with currency.
/// </summary>
public class Money : ValueObject
{
    /// <summary>
    /// The monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// The currency code (e.g., "USD", "EUR", "RSD").
    /// </summary>
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a new Money instance.
    /// </summary>
    /// <param name="amount">Amount (must be >= 0)</param>
    /// <param name="currency">Currency code (default: RSD)</param>
    public static Money Create(decimal amount, string currency = "RSD")
    {
        if (amount < 0)
            throw new BookingDomainException("Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new BookingDomainException("Currency must be specified.");

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Returns zero amount for the specified currency.
    /// </summary>
    public static Money Zero(string currency = "RSD") => new(0, currency);

    /// <summary>
    /// Adds two monetary amounts.
    /// </summary>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new BookingDomainException("Cannot add amounts in different currencies.");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Multiplies amount by a number (e.g., for calculating total price).
    /// </summary>
    public static Money operator *(Money money, int multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
