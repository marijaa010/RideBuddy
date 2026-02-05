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
    Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bookings for a specific passenger.
    /// </summary>
    Task<IReadOnlyList<BookingEntity>> GetByPassengerIdAsync(
        Guid passengerId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bookings for a specific ride.
    /// </summary>
    Task<IReadOnlyList<BookingEntity>> GetByRideIdAsync(
        Guid rideId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bookings by status for a specific passenger.
    /// </summary>
    Task<IReadOnlyList<BookingEntity>> GetByPassengerAndStatusAsync(
        Guid passengerId, 
        BookingStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an active booking exists for a passenger on a specific ride.
    /// </summary>
    Task<bool> ExistsActiveBookingAsync(
        Guid passengerId, 
        Guid rideId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new booking.
    /// </summary>
    Task AddAsync(BookingEntity booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing booking.
    /// </summary>
    Task UpdateAsync(BookingEntity booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of booked seats for a specific ride.
    /// </summary>
    Task<int> GetTotalBookedSeatsForRideAsync(
        Guid rideId, 
        CancellationToken cancellationToken = default);
}
