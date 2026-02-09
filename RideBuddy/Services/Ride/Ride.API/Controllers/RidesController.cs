using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ride.Application.Commands.CancelRide;
using Ride.Application.Commands.CompleteRide;
using Ride.Application.Commands.CreateRide;
using Ride.Application.Commands.StartRide;
using Ride.Application.DTOs;
using Ride.Application.Queries.GetRideById;
using Ride.Application.Queries.GetRidesByDriver;
using Ride.Application.Queries.SearchRides;

namespace Ride.API.Controllers;

/// <summary>
/// API controller for managing rides.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RidesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RidesController> _logger;

    public RidesController(IMediator mediator, ILogger<RidesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new ride (driver only).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RideDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRide([FromBody] CreateRideRequest request)
    {
        var command = new CreateRideCommand
        {
            DriverId = GetUserIdFromToken(),
            OriginName = request.OriginName,
            OriginLatitude = request.OriginLatitude,
            OriginLongitude = request.OriginLongitude,
            DestinationName = request.DestinationName,
            DestinationLatitude = request.DestinationLatitude,
            DestinationLongitude = request.DestinationLongitude,
            DepartureTime = request.DepartureTime,
            AvailableSeats = request.AvailableSeats,
            PricePerSeat = request.PricePerSeat,
            Currency = request.Currency,
            AutoConfirmBookings = request.AutoConfirmBookings
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create ride: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(GetRideById),
            new { id = result.Value.Id },
            result.Value);
    }

    /// <summary>
    /// Gets a ride by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RideDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRideById(Guid id)
    {
        var query = new GetRideByIdQuery { RideId = id };
        var ride = await _mediator.Send(query);

        if (ride is null)
            return NotFound(new { error = $"Ride with ID '{id}' not found." });

        return Ok(ride);
    }

    /// <summary>
    /// Searches for available rides.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<RideDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchRides(
        [FromQuery] string? origin = null,
        [FromQuery] string? destination = null,
        [FromQuery] DateTime? date = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new SearchRidesQuery
        {
            Origin = origin,
            Destination = destination,
            Date = date,
            Page = page,
            PageSize = pageSize
        };

        var rides = await _mediator.Send(query);
        return Ok(rides);
    }

    /// <summary>
    /// Gets all rides for the current driver.
    /// </summary>
    [HttpGet("my-rides")]
    [ProducesResponseType(typeof(IReadOnlyList<RideDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRides()
    {
        var query = new GetRidesByDriverQuery { DriverId = GetUserIdFromToken() };
        var rides = await _mediator.Send(query);
        return Ok(rides);
    }

    /// <summary>
    /// Starts a scheduled ride (driver only).
    /// </summary>
    [HttpPut("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartRide(Guid id)
    {
        var command = new StartRideCommand
        {
            RideId = id,
            DriverId = GetUserIdFromToken()
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to start ride {RideId}: {Error}", id, result.Error);
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Completes an in-progress ride (driver only).
    /// </summary>
    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteRide(Guid id)
    {
        var command = new CompleteRideCommand
        {
            RideId = id,
            DriverId = GetUserIdFromToken()
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to complete ride {RideId}: {Error}", id, result.Error);
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Cancels a ride (driver only).
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelRide(Guid id, [FromBody] CancelRideRequest? request = null)
    {
        var command = new CancelRideCommand
        {
            RideId = id,
            DriverId = GetUserIdFromToken(),
            Reason = request?.Reason ?? string.Empty
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to cancel ride {RideId}: {Error}", id, result.Error);
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user ID in token.");
        }

        return userId;
    }
}

// --- Request Models ---

public record CreateRideRequest
{
    public string OriginName { get; init; } = string.Empty;
    public double OriginLatitude { get; init; }
    public double OriginLongitude { get; init; }
    public string DestinationName { get; init; } = string.Empty;
    public double DestinationLatitude { get; init; }
    public double DestinationLongitude { get; init; }
    public DateTime DepartureTime { get; init; }
    public int AvailableSeats { get; init; }
    public decimal PricePerSeat { get; init; }
    public string Currency { get; init; } = "RSD";
    public bool AutoConfirmBookings { get; init; } = true;
}

public record CancelRideRequest
{
    public string? Reason { get; init; }
}
