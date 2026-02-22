namespace RideBuddy.E2E.Tests.Infrastructure;

public class TestUserContext
{
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string AccessToken { get; init; } = null!;
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Role { get; init; } = null!;
}
