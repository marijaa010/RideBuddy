namespace Ride.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a ride.
/// </summary>
public enum RideStatus
{
    /// <summary>
    /// Ride is scheduled and accepting bookings.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Ride is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Ride has been completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Ride has been cancelled by the driver.
    /// </summary>
    Cancelled = 3
}
