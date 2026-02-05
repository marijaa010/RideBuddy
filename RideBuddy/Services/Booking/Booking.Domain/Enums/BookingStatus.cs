namespace Booking.Domain.Enums;

/// <summary>
/// Represents the status of a booking.
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Booking has been created and is awaiting confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Booking has been confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Booking has been cancelled.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Ride has been completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Booking was rejected.
    /// </summary>
    Rejected = 4
}
