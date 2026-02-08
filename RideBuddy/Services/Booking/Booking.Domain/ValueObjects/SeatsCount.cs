using Booking.Domain.Common;
using Booking.Domain.Exceptions;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Value Object representing the number of booked seats.
/// </summary>
public class SeatsCount : ValueObject
{
    public int Value { get; }

    private SeatsCount(int value)
    {
        Value = value;
    }

    public static SeatsCount Create(int value)
    {
        if (value <= 0)
            throw new BookingDomainException("Number of seats must be greater than 0.");

        return new SeatsCount(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(SeatsCount seatsCount) => seatsCount.Value;
}
