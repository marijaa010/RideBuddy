using SharedKernel;
using Ride.Domain.Exceptions;

namespace Ride.Domain.ValueObjects;

/// <summary>
/// Value Object representing a monetary amount with currency.
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new RideDomainException("Amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new RideDomainException("Currency must be specified.");

        var trimmed = currency.Trim();
        if (trimmed.Length != 3 || !trimmed.All(char.IsLetter))
            throw new RideDomainException("Currency must be a three-letter alphabetic code (ISO 4217).");

        return new Money(amount, trimmed.ToUpperInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}
