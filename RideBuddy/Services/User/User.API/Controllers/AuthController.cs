using MediatR;
using Microsoft.AspNetCore.Mvc;
using User.Application.Commands.RegisterUser;
using User.Application.DTOs;
using User.Application.Queries.LoginUser;

namespace User.API.Controllers;

/// <summary>
/// Authentication controller for registration and login.
/// Inspired by the course's AuthenticationController but dispatches through MediatR.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogWarning("Registration failed: {Error}", result.Error);

            if (result.Error.Contains("already exists"))
                return Conflict(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(
            nameof(UsersController.GetUser),
            "Users",
            new { id = result.Value.User.Id },
            result.Value);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var query = new LoginUserQuery
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            _logger.LogWarning("Login failed for {Email}: {Error}", request.Email, result.Error);
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}

/// <summary>
/// Request model for user registration.
/// </summary>
public record RegisterRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Role { get; init; } = "Passenger";
}

/// <summary>
/// Request model for user login.
/// </summary>
public record LoginRequest
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}
