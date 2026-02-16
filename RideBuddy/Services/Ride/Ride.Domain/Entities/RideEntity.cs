using SharedKernel;
using Ride.Domain.Enums;
using Ride.Domain.Events;
using Ride.Domain.Exceptions;
using Ride.Domain.ValueObjects;

namespace Ride.Domain.Entities;

/// <summary>
/// Aggregate Root representing a ride offered by a driver.
/// </summary>
public class RideEntity : AggregateRoot
{
    /// <summary>
    /// ID of the driver offering the ride.
    /// </summary>
    public DriverId DriverId { get; private set; } = null!;

    /// <summary>
    /// Driver's first name.
    /// </summary>
    public string DriverFirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Driver's last name.
    /// </summary>
    public string DriverLastName { get; private set; } = string.Empty;

    /// <summary>
    /// Origin location of the ride.
    /// </summary>
    public Location Origin { get; private set; } = null!;

    /// <summary>
    /// Destination location of the ride.
    /// </summary>
    public Location Destination { get; private set; } = null!;

    /// <summary>
    /// Scheduled departure time.
    /// </summary>
    public DateTime DepartureTime { get; private set; }

    /// <summary>
    /// Total number of seats offered by the driver.
    /// </summary>
    public SeatsCount TotalSeats { get; private set; } = null!;

    /// <summary>
    /// Currently available (unreserved) seats.
    /// </summary>
    public SeatsCount AvailableSeats { get; private set; } = null!;

    /// <summary>
    /// Price per seat.
    /// </summary>
    public Money PricePerSeat { get; private set; } = null!;

    /// <summary>
    /// Current status of the ride.
    /// </summary>
    public RideStatus Status { get; private set; }

    /// <summary>
    /// Whether new bookings are automatically confirmed.
    /// </summary>
    public bool AutoConfirmBookings { get; private set; }

    /// <summary>
    /// When the ride was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the ride was started.
    /// </summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>
    /// When the ride was completed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// When the ride was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Reason for cancellation (if cancelled).
    /// </summary>
    public string? CancellationReason { get; private set; }

    // Private constructor for EF Core
    private RideEntity() { }

    /// <summary>
    /// Factory method to create a new ride.
    /// </summary>
    public static RideEntity Create(
        Guid driverId,
        string driverFirstName,
        string driverLastName,
        string originName, double originLat, double originLng,
        string destinationName, double destLat, double destLng,
        DateTime departureTime,
        int availableSeats,
        decimal pricePerSeat,
        string currency,
        bool autoConfirmBookings = true)
    {
        if (departureTime <= DateTime.UtcNow)
            throw new RideDomainException("Departure time must be in the future.");

        var ride = new RideEntity
        {
            Id = Guid.NewGuid(),
            DriverId = DriverId.Create(driverId),
            DriverFirstName = driverFirstName,
            DriverLastName = driverLastName,
            Origin = Location.Create(originName, originLat, originLng),
            Destination = Location.Create(destinationName, destLat, destLng),
            DepartureTime = departureTime,
            TotalSeats = SeatsCount.Create(availableSeats),
            AvailableSeats = SeatsCount.Create(availableSeats),
            PricePerSeat = Money.Create(pricePerSeat, currency),
            Status = RideStatus.Scheduled,
            AutoConfirmBookings = autoConfirmBookings,
            CreatedAt = DateTime.UtcNow
        };

        ride.AddDomainEvent(new RideCreatedEvent(
            ride.Id, driverId,
            originName, destinationName,
            departureTime, availableSeats,
            pricePerSeat, currency));

        return ride;
    }

    /// <summary>
    /// Reserves a given number of seats on this ride.
    /// </summary>
    public void ReserveSeats(int count)
    {
        if (Status != RideStatus.Scheduled)
            throw new RideDomainException($"Cannot reserve seats on a ride with status '{Status}'.");

        if (count <= 0)
            throw new RideDomainException("Seats to reserve must be greater than 0.");

        if (AvailableSeats.Value < count)
            throw new RideDomainException(
                $"Not enough available seats. Requested: {count}, Available: {AvailableSeats.Value}.");

        AvailableSeats = SeatsCount.Create(AvailableSeats.Value - count);
        IncrementVersion();

        AddDomainEvent(new SeatsReservedEvent(Id, count, AvailableSeats.Value));
    }

    /// <summary>
    /// Releases previously reserved seats back to availability.
    /// </summary>
    public void ReleaseSeats(int count)
    {
        if (count <= 0)
            throw new RideDomainException("Seats to release must be greater than 0.");

        var newAvailable = AvailableSeats.Value + count;
        if (newAvailable > TotalSeats.Value)
            throw new RideDomainException(
                $"Cannot release more seats than total. Total: {TotalSeats.Value}, Would become: {newAvailable}.");

        AvailableSeats = SeatsCount.Create(newAvailable);
        IncrementVersion();

        AddDomainEvent(new SeatsReleasedEvent(Id, count, AvailableSeats.Value));
    }

    /// <summary>
    /// Starts the ride.
    /// </summary>
    public void Start()
    {
        if (Status != RideStatus.Scheduled)
            throw new RideDomainException($"Only scheduled rides can be started. Current status: '{Status}'.");

        Status = RideStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new RideStartedEvent(Id, DriverId.Value, StartedAt.Value));
    }

    /// <summary>
    /// Completes the ride.
    /// </summary>
    public void Complete()
    {
        if (Status != RideStatus.InProgress)
            throw new RideDomainException($"Only in-progress rides can be completed. Current status: '{Status}'.");

        Status = RideStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new RideCompletedEvent(Id, DriverId.Value, CompletedAt.Value));
    }

    /// <summary>
    /// Cancels the ride.
    /// </summary>
    public void Cancel(string reason)
    {
        if (Status == RideStatus.Completed)
            throw new RideDomainException("Cannot cancel a completed ride.");

        if (Status == RideStatus.Cancelled)
            throw new RideDomainException("Ride is already cancelled.");

        Status = RideStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        IncrementVersion();

        AddDomainEvent(new RideCancelledEvent(Id, DriverId.Value, reason, CancelledAt.Value));
    }

    /// <summary>
    /// Checks if the ride is available for booking.
    /// </summary>
    public bool IsAvailable => Status == RideStatus.Scheduled && AvailableSeats.Value > 0 && DepartureTime > DateTime.UtcNow;
}
