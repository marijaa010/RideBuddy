using SharedKernel;
using Ride.Domain.Exceptions;

namespace Ride.Domain.ValueObjects;

/// <summary>
/// Strongly-typed ID for a driver.
/// </summary>
public class DriverId : ValueObject
{
    public Guid Value { get; }

    private DriverId(Guid value) { Value = value; }

    public static DriverId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new RideDomainException("DriverId cannot be empty.");
        return new DriverId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(DriverId id) => id.Value;
}
