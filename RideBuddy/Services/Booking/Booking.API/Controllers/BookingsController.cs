using Booking.Application.Commands.CancelBooking;
using Booking.Application.Commands.CreateBooking;
using Booking.Application.DTOs;
using Booking.Application.Queries.GetBookingById;
using Booking.Application.Queries.GetBookingsByPassenger;
using Booking.Application.Queries.GetBookingsByRide;
using Booking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.API.Controllers;

/// <summary>
/// API controller for managing ride bookings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
// [Authorize] // TODO: Re-enable after User Service is ready
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IMediator mediator, ILogger<BookingsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking for a ride.
    /// </summary>
    /// <param name="request">Booking details</param>
    /// <returns>Created booking</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var command = new CreateBookingCommand
        {
            RideId = request.RideId,
            PassengerId = GetUserIdFromToken(),
            SeatsToBook = request.SeatsToBook
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create booking: {Error}", result.Error);
            return Conflict(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Gets a booking by its ID.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var query = new GetBookingByIdQuery { BookingId = id };
        var booking = await _mediator.Send(query);

        if (booking is null)
        {
            return NotFound(new { error = $"Booking with ID '{id}' not found." });
        }

        return Ok(booking);
    }

    /// <summary>
    /// Gets all bookings for the current user (passenger).
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of bookings</returns>
    [HttpGet("my-bookings")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings([FromQuery] BookingStatus? status = null)
    {
        var query = new GetBookingsByPassengerQuery
        {
            PassengerId = GetUserIdFromToken(),
            Status = status
        };

        var bookings = await _mediator.Send(query);
        return Ok(bookings);
    }

    /// <summary>
    /// Gets all bookings for a specific ride.
    /// </summary>
    /// <param name="rideId">Ride ID</param>
    /// <returns>List of bookings</returns>
    [HttpGet("by-ride/{rideId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookingsByRide(Guid rideId)
    {
        var query = new GetBookingsByRideQuery { RideId = rideId };
        var bookings = await _mediator.Send(query);
        return Ok(bookings);
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Cancellation reason (optional)</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(Guid id, [FromBody] CancelBookingRequest? request = null)
    {
        var command = new CancelBookingCommand
        {
            BookingId = id,
            UserId = GetUserIdFromToken(),
            Reason = request?.Reason ?? string.Empty
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to cancel booking {BookingId}: {Error}", id, result.Error);
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    private Guid GetUserIdFromToken()
    {
        // Extract user ID from JWT claims
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            // TODO: Remove this fallback after User Service is ready
            // Return test user ID for development
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        return userId;
    }
}

/// <summary>
/// Request model for creating a booking.
/// </summary>
public record CreateBookingRequest
{
    public Guid RideId { get; init; }
    public int SeatsToBook { get; init; } = 1;
}

/// <summary>
/// Request model for cancelling a booking.
/// </summary>
public record CancelBookingRequest
{
    public string? Reason { get; init; }
}
