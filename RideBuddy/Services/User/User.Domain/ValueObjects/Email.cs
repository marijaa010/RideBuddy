using System.Text.RegularExpressions;
using SharedKernel;
using User.Domain.Exceptions;

namespace User.Domain.ValueObjects;

/// <summary>
/// Value object representing a validated email address.
/// </summary>
public partial class Email : ValueObject
{
    private static readonly Regex EmailRegex = MyEmailRegex();

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new UserDomainException("Email cannot be empty.");

        var trimmed = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(trimmed))
            throw new UserDomainException($"'{value}' is not a valid email address.");

        return new Email(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex MyEmailRegex();
}
