using RideBuddy.E2E.Tests.Models;

namespace RideBuddy.E2E.Tests.Infrastructure;

public abstract class E2ETestBase : IAsyncLifetime, IDisposable
{
    protected readonly ApiClient Api;
    private readonly string _testSuffix;

    protected E2ETestBase()
    {
        Api = new ApiClient();
        _testSuffix = Guid.NewGuid().ToString("N")[..8];
    }

    public virtual Task InitializeAsync()
    {
        SkipIfNotEnabled();
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => Api.Dispose();

    private static void SkipIfNotEnabled()
    {
        var enabled = Environment.GetEnvironmentVariable("RUN_RIDEBUDDY_E2E_TESTS");
        Skip.If(enabled != "1", "E2E tests are disabled. Set RUN_RIDEBUDDY_E2E_TESTS=1 to run.");
    }

    protected string UniqueEmail(string prefix) =>
        $"{prefix}.{_testSuffix}@e2etest.com";

    protected async Task<TestUserContext> RegisterAndLoginDriver(string emailPrefix = "driver")
    {
        return await RegisterAndLogin(emailPrefix, "Driver");
    }

    protected async Task<TestUserContext> RegisterAndLoginPassenger(string emailPrefix = "passenger")
    {
        return await RegisterAndLogin(emailPrefix, "Passenger");
    }

    private async Task<TestUserContext> RegisterAndLogin(string emailPrefix, string role)
    {
        var email = UniqueEmail(emailPrefix);
        var password = "TestPass123!";

        await Api.Register(new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = role,
            LastName = "E2E",
            PhoneNumber = $"+38160{Random.Shared.Next(1000000, 9999999)}",
            Role = role
        });

        var loginResponse = await Api.Login(new LoginRequest
        {
            Email = email,
            Password = password
        });

        return new TestUserContext
        {
            Email = email,
            Password = password,
            AccessToken = loginResponse.AccessToken,
            UserId = loginResponse.User.Id,
            FirstName = role,
            LastName = "E2E",
            Role = role
        };
    }

    protected static CreateRideRequest MakeRideRequest(
        bool autoConfirm = true,
        int seats = 3,
        decimal price = 10m,
        DateTime? departureTime = null)
    {
        return new CreateRideRequest
        {
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = departureTime ?? DateTime.UtcNow.AddHours(2),
            AvailableSeats = seats,
            PricePerSeat = price,
            Currency = "RSD",
            AutoConfirmBookings = autoConfirm
        };
    }

    protected static async Task WaitForCondition(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null,
        string? failureMessage = null)
    {
        var totalTimeout = timeout ?? TimeSpan.FromSeconds(15);
        var pollInterval = interval ?? TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + totalTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(pollInterval);
        }

        throw new TimeoutException(
            failureMessage ?? "Condition was not met within the timeout period.");
    }
}
