using SharedKernel;
using User.Domain.Exceptions;

namespace User.Domain.ValueObjects;

/// <summary>
/// Strongly-typed identifier for a user.
/// </summary>
public class UserId : ValueObject
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        Value = value;
    }

    public static UserId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new UserDomainException("UserId cannot be empty.");

        return new UserId(value);
    }

    public static UserId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserId userId) => userId.Value;
}
