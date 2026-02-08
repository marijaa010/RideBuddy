using System.Text.RegularExpressions;
using SharedKernel;
using User.Domain.Exceptions;

namespace User.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated phone number.
/// </summary>
public partial class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = MyPhoneRegex();

    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new UserDomainException("Phone number cannot be empty.");

        var trimmed = value.Trim();

        if (!PhoneRegex.IsMatch(trimmed))
            throw new UserDomainException($"'{value}' is not a valid phone number.");

        return new PhoneNumber(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    [GeneratedRegex(@"^\+?[\d\s\-()]{7,20}$", RegexOptions.Compiled)]
    private static partial Regex MyPhoneRegex();
}
