using RideBuddy.E2E.Tests.Infrastructure;
using RideBuddy.E2E.Tests.Models;

namespace RideBuddy.E2E.Tests.Tests;

public class RideLifecycleTests : E2ETestBase
{
    [SkippableFact]
    public async Task CreateRide_AsDriver_Returns201WithCorrectFields()
    {
        var driver = await RegisterAndLoginDriver();
        var request = MakeRideRequest(seats: 4, price: 15m);

        var response = await Api.CreateRideRaw(request, driver.AccessToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ride = await response.Content.ReadFromJsonAsync<RideResponse>();
        ride.Should().NotBeNull();
        ride!.Id.Should().NotBeEmpty();
        ride.DriverId.Should().Be(driver.UserId);
        ride.OriginName.Should().Be("Belgrade");
        ride.DestinationName.Should().Be("Novi Sad");
        ride.AvailableSeats.Should().Be(4);
        ride.PricePerSeat.Should().Be(15m);
        ride.Currency.Should().Be("RSD");
        ride.Status.Should().Be(0); // Scheduled
    }

    [SkippableFact]
    public async Task GetRideById_Anonymous_Returns200()
    {
        var driver = await RegisterAndLoginDriver();
        var ride = await Api.CreateRide(MakeRideRequest(), driver.AccessToken);

        var fetched = await Api.GetRide(ride.Id);

        fetched.Id.Should().Be(ride.Id);
        fetched.OriginName.Should().Be("Belgrade");
        fetched.DestinationName.Should().Be("Novi Sad");
    }

    [SkippableFact]
    public async Task StartRide_TransitionsToInProgress()
    {
        var driver = await RegisterAndLoginDriver();
        var ride = await Api.CreateRide(MakeRideRequest(), driver.AccessToken);

        var response = await Api.StartRide(ride.Id, driver.AccessToken);
        response.EnsureSuccessStatusCode();

        var updated = await Api.GetRide(ride.Id);
        updated.Status.Should().Be(1); // InProgress
    }

    [SkippableFact]
    public async Task CompleteRide_AfterStart_TransitionsToCompleted()
    {
        var driver = await RegisterAndLoginDriver();
        var ride = await Api.CreateRide(MakeRideRequest(), driver.AccessToken);

        await Api.StartRide(ride.Id, driver.AccessToken);
        var response = await Api.CompleteRide(ride.Id, driver.AccessToken);
        response.EnsureSuccessStatusCode();

        var updated = await Api.GetRide(ride.Id);
        updated.Status.Should().Be(2); // Completed
    }

    [SkippableFact]
    public async Task CancelRide_TransitionsToCancelled()
    {
        var driver = await RegisterAndLoginDriver();
        var ride = await Api.CreateRide(MakeRideRequest(), driver.AccessToken);

        var response = await Api.CancelRide(ride.Id, driver.AccessToken, "E2E test cancellation");
        response.EnsureSuccessStatusCode();

        var updated = await Api.GetRide(ride.Id);
        updated.Status.Should().Be(3); // Cancelled
    }
}
