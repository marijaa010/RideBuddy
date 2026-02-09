using SharedKernel;
using Ride.Domain.Exceptions;

namespace Ride.Domain.ValueObjects;

/// <summary>
/// Strongly-typed ID for a ride.
/// </summary>
public class RideId : ValueObject
{
    public Guid Value { get; }

    private RideId(Guid value) { Value = value; }

    public static RideId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new RideDomainException("RideId cannot be empty.");
        return new RideId(value);
    }

    public static RideId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(RideId id) => id.Value;
}
