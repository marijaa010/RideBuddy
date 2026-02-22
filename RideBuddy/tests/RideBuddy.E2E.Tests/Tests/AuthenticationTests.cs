using RideBuddy.E2E.Tests.Infrastructure;
using RideBuddy.E2E.Tests.Models;

namespace RideBuddy.E2E.Tests.Tests;

public class AuthenticationTests : E2ETestBase
{
    [SkippableFact]
    public async Task Register_WithValidData_Returns201AndAccessToken()
    {
        var request = new RegisterRequest
        {
            Email = UniqueEmail("register"),
            Password = "TestPass123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+381601234567",
            Role = "Driver"
        };

        var response = await Api.RegisterRaw(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(request.Email);
        auth.User.FirstName.Should().Be("John");
        auth.User.LastName.Should().Be("Doe");
        auth.User.Role.Should().Be("Driver");
    }

    [SkippableFact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = UniqueEmail("duplicate");
        var request = new RegisterRequest
        {
            Email = email,
            Password = "TestPass123!",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+381601234568",
            Role = "Passenger"
        };

        var first = await Api.RegisterRaw(request);
        first.EnsureSuccessStatusCode();

        var second = await Api.RegisterRaw(request);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var email = UniqueEmail("login");
        var password = "TestPass123!";

        await Api.Register(new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = "Login",
            LastName = "Test",
            PhoneNumber = "+381601234569",
            Role = "Passenger"
        });

        var response = await Api.LoginRaw(new LoginRequest
        {
            Email = email,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Email.Should().Be(email);
    }

    [SkippableFact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = UniqueEmail("wrongpw");

        await Api.Register(new RegisterRequest
        {
            Email = email,
            Password = "TestPass123!",
            FirstName = "Wrong",
            LastName = "Password",
            PhoneNumber = "+381601234570",
            Role = "Passenger"
        });

        var response = await Api.LoginRaw(new LoginRequest
        {
            Email = email,
            Password = "WrongPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
