using SharedKernel;
using Booking.Domain.Exceptions;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Id for a booking.
/// </summary>
public class BookingId : ValueObject
{
    public Guid Value { get; }

    private BookingId(Guid value)
    {
        Value = value;
    }

    public static BookingId Create(Guid value)
    {
        if (value == Guid.Empty)
            throw new BookingDomainException("BookingId cannot be empty.");

        return new BookingId(value);
    }

    public static BookingId CreateNew() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(BookingId bookingId) => bookingId.Value;
}
