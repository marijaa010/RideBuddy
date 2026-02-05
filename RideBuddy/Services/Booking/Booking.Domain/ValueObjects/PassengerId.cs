using Booking.Domain.Common;
using Booking.Domain.Exceptions;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// ID for a passenger.
/// </summary>
public class PassengerId : ValueObject
{
    public Guid Value { get; }

    private PassengerId(Guid value)
    {
        Value = value;
    }

    public static PassengerId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new BookingDomainException("PassengerId cannot be empty.");

        return new PassengerId(value);
    }

    public static PassengerId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PassengerId passengerId) => passengerId.Value;
}
