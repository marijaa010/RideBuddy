using SharedKernel;
using Ride.Domain.Exceptions;

namespace Ride.Domain.ValueObjects;

/// <summary>
/// Value Object representing the number of available seats.
/// </summary>
public class SeatsCount : ValueObject
{
    public int Value { get; }

    private SeatsCount(int value) { Value = value; }

    public static SeatsCount Create(int value)
    {
        if (value < 0)
            throw new RideDomainException("Seat count cannot be negative.");
        return new SeatsCount(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
    public static implicit operator int(SeatsCount s) => s.Value;
}
