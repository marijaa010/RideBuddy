using Booking.Domain.Entities;
using Booking.Domain.Enums;

namespace Booking.Domain.Interfaces;

/// <summary>
/// Repository interface for accessing bookings.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Gets a booking by its ID.
    /// </summary>
    Task<BookingEntity?> GetById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bookings for a specific passenger.
    /// </summary>
    Task<IReadOnlyList<BookingEntity>> GetByPassengerId(
        Guid passengerId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bookings for a specific ride.
    /// </summary>
    Task<IReadOnlyList<BookingEntity>> GetByRideId(
        Guid rideId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bookings by status for a specific passenger.
    /// </summary>
    Task<IReadOnlyList<BookingEntity>> GetByPassengerAndStatus(
        Guid passengerId, 
        BookingStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an active booking exists for a passenger on a specific ride.
    /// </summary>
    Task<bool> ExistsActiveBooking(
        Guid passengerId, 
        Guid rideId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new booking.
    /// </summary>
    Task Add(BookingEntity booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing booking.
    /// </summary>
    Task Update(BookingEntity booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of booked seats for a specific ride.
    /// </summary>
    Task<int> GetTotalBookedSeatsForRide(
        Guid rideId, 
        CancellationToken cancellationToken = default);
}
