using RideBuddy.E2E.Tests.Infrastructure;
using RideBuddy.E2E.Tests.Models;

namespace RideBuddy.E2E.Tests.Tests;

public class BookingFlowTests : E2ETestBase
{
    [SkippableFact]
    public async Task CreateBooking_AutoConfirm_ReturnsConfirmedStatus()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        booking.Status.Should().Be(1); // Confirmed
        booking.RideId.Should().Be(ride.Id);
        booking.PassengerId.Should().Be(passenger.UserId);
        booking.SeatsBooked.Should().Be(1);
        booking.ConfirmedAt.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task CreateBooking_ManualConfirm_ReturnsPendingStatus()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: false), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        booking.Status.Should().Be(0); // Pending
        booking.ConfirmedAt.Should().BeNull();
    }

    [SkippableFact]
    public async Task ConfirmBooking_DriverConfirms_TransitionsToConfirmed()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: false), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        booking.Status.Should().Be(0); // Pending

        var confirmResponse = await Api.ConfirmBooking(booking.Id, driver.AccessToken);
        confirmResponse.EnsureSuccessStatusCode();

        var updated = await Api.GetBooking(booking.Id, driver.AccessToken);
        updated.Status.Should().Be(1); // Confirmed
        updated.ConfirmedAt.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task RejectBooking_DriverRejects_TransitionsToRejected()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: false), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        var rejectResponse = await Api.RejectBooking(booking.Id, driver.AccessToken, "No room");
        rejectResponse.EnsureSuccessStatusCode();

        var updated = await Api.GetBooking(booking.Id, driver.AccessToken);
        updated.Status.Should().Be(4); // Rejected
    }

    [SkippableFact]
    public async Task CancelBooking_RestoresSeats()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true, seats: 3), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        var rideAfterBooking = await Api.GetRide(ride.Id);
        rideAfterBooking.AvailableSeats.Should().Be(2);

        var cancelResponse = await Api.CancelBooking(booking.Id, passenger.AccessToken, "Changed plans");
        cancelResponse.EnsureSuccessStatusCode();

        var rideAfterCancel = await Api.GetRide(ride.Id);
        rideAfterCancel.AvailableSeats.Should().Be(3);
    }

    [SkippableFact]
    public async Task GetMyBookings_ReturnsPassengersBookings()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        var myBookings = await Api.GetMyBookings(passenger.AccessToken);

        myBookings.Should().Contain(b => b.Id == booking.Id);
    }

    [SkippableFact]
    public async Task CreateBooking_WithoutAuth_Returns401()
    {
        var response = await Api.CreateBookingRaw(
            new CreateBookingRequest { RideId = Guid.NewGuid(), SeatsToBook = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
