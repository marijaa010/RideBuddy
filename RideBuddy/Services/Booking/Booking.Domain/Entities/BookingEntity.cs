using SharedKernel;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;

namespace Booking.Domain.Entities;

/// <summary>
/// Aggregate Root representing a ride booking.
/// This is the main entity of the Booking bounded context.
/// </summary>
public class BookingEntity : AggregateRoot
{
    /// <summary>
    /// ID of the booked ride.
    /// </summary>
    public RideId RideId { get; private set; } = null!;

    /// <summary>
    /// ID of the passenger who made the booking.
    /// </summary>
    public PassengerId PassengerId { get; private set; } = null!;

    /// <summary>
    /// Passenger's first name.
    /// </summary>
    public string PassengerFirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Passenger's last name.
    /// </summary>
    public string PassengerLastName { get; private set; } = string.Empty;

    /// <summary>
    /// Number of seats booked.
    /// </summary>
    public SeatsCount SeatsBooked { get; private set; } = null!;

    /// <summary>
    /// Total price of the booking.
    /// </summary>
    public Money TotalPrice { get; private set; } = null!;

    /// <summary>
    /// Current status of the booking.
    /// </summary>
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Timestamp when the booking was created.
    /// </summary>
    public DateTime BookedAt { get; private set; }

    /// <summary>
    /// Timestamp when the booking was confirmed.
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// Timestamp when the booking was cancelled.
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// Timestamp when the ride was completed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Timestamp when the booking was rejected.
    /// </summary>
    public DateTime? RejectedAt { get; private set; }

    /// <summary>
    /// Reason for cancellation (if cancelled).
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Driver ID.
    /// </summary>
    public Guid DriverId { get; private set; }

    // Private constructor for EF Core
    private BookingEntity() { }

    /// <summary>
    /// Factory method to create a new booking.
    /// </summary>
    public static BookingEntity Create(
        Guid rideId,
        Guid passengerId,
        string passengerFirstName,
        string passengerLastName,
        int seatsBooked,
        decimal pricePerSeat,
        string currency,
        Guid driverId,
        bool isAutoConfirmed = false)
    {
        var booking = new BookingEntity
        {
            Id = Guid.NewGuid(),
            RideId = RideId.Create(rideId),
            PassengerId = PassengerId.Create(passengerId),
            PassengerFirstName = passengerFirstName,
            PassengerLastName = passengerLastName,
            SeatsBooked = SeatsCount.Create(seatsBooked),
            TotalPrice = Money.Create(pricePerSeat * seatsBooked, currency),
            Status = BookingStatus.Pending,
            BookedAt = DateTime.UtcNow,
            DriverId = driverId
        };

        booking.AddDomainEvent(new BookingCreatedEvent(
            booking.Id,
            rideId,
            passengerId,
            driverId,
            seatsBooked,
            booking.TotalPrice.Amount,
            booking.TotalPrice.Currency,
            isAutoConfirmed));

        return booking;
    }

    /// <summary>
    /// Confirms the booking after the Ride Service has reserved the seats.
    /// </summary>
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new BookingDomainException(
                $"Booking cannot be confirmed because it is in '{Status}' status.");

        Status = BookingStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new BookingConfirmedEvent(
            Id,
            RideId.Value,
            PassengerId.Value,
            SeatsBooked.Value,
            TotalPrice.Amount,
            ConfirmedAt.Value));
    }

    /// <summary>
    /// Cancels the booking.
    /// </summary>
    /// <param name="reason">Reason for cancellation</param>
    /// <param name="cancelledByPassenger">True if the passenger initiated the cancellation</param>
    /// <param name="departureTime">Departure time of the ride, used for late-cancellation notifications</param>
    public void Cancel(string reason, bool cancelledByPassenger, DateTime departureTime)
    {
        if (Status == BookingStatus.Cancelled)
            throw new BookingDomainException("Booking is already cancelled.");

        if (Status == BookingStatus.Completed)
            throw new BookingDomainException("Cannot cancel a completed ride.");

        Status = BookingStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        IncrementVersion();

        AddDomainEvent(new BookingCancelledEvent(
            Id,
            RideId.Value,
            PassengerId.Value,
            DriverId,
            SeatsBooked.Value,
            reason,
            CancelledAt.Value,
            cancelledByPassenger,
            departureTime));
    }

    /// <summary>
    /// Marks the booking as rejected by the driver.
    /// </summary>
    /// <param name="reason">Reason for rejection</param>
    public void Reject(string reason)
    {
        if (Status != BookingStatus.Pending)
            throw new BookingDomainException(
                $"Only bookings in 'Pending' status can be rejected. Current status: '{Status}'.");

        Status = BookingStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        CancellationReason = reason;
        IncrementVersion();

        AddDomainEvent(new BookingRejectedEvent(
            Id,
            RideId.Value,
            PassengerId.Value,
            SeatsBooked.Value,
            reason,
            RejectedAt.Value));
    }

    /// <summary>
    /// Marks the booking as completed after the ride is finished.
    /// </summary>
    public void Complete()
    {
        if (Status != BookingStatus.Confirmed)
            throw new BookingDomainException(
                $"Only confirmed bookings can be marked as completed. Current status: '{Status}'.");

        Status = BookingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        IncrementVersion();

        AddDomainEvent(new BookingCompletedEvent(
            Id,
            RideId.Value,
            PassengerId.Value,
            CompletedAt.Value));
    }

    /// <summary>
    /// Checks if the booking can be cancelled.
    /// </summary>
    public bool CanBeCancelled()
    {
        return Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;
    }
}
