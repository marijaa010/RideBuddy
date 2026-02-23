using RideBuddy.E2E.Tests.Infrastructure;
using RideBuddy.E2E.Tests.Models;

namespace RideBuddy.E2E.Tests.Tests;

public class CrossServiceTests : E2ETestBase
{
    [SkippableFact]
    public async Task Booking_ReducesRideAvailableSeats()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true, seats: 3), driver.AccessToken);

        ride.AvailableSeats.Should().Be(3);

        await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        var updated = await Api.GetRide(ride.Id);
        updated.AvailableSeats.Should().Be(2);
    }

    [SkippableFact]
    public async Task BookingCancellation_RestoresRideAvailableSeats()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true, seats: 3), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        var afterBooking = await Api.GetRide(ride.Id);
        afterBooking.AvailableSeats.Should().Be(2);

        await Api.CancelBooking(booking.Id, passenger.AccessToken, "E2E test");

        var afterCancel = await Api.GetRide(ride.Id);
        afterCancel.AvailableSeats.Should().Be(3);
    }

    [SkippableFact]
    public async Task MultipleBookings_ReduceSeatsCorrectly()
    {
        var driver = await RegisterAndLoginDriver();
        var passengerA = await RegisterAndLoginPassenger("passengerA");
        var passengerB = await RegisterAndLoginPassenger("passengerB");
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true, seats: 4), driver.AccessToken);

        await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 2 },
            passengerA.AccessToken);

        await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passengerB.AccessToken);

        var updated = await Api.GetRide(ride.Id);
        updated.AvailableSeats.Should().Be(1);
    }

    [SkippableFact]
    public async Task Booking_WhenNotEnoughSeats_Fails()
    {
        var driver = await RegisterAndLoginDriver();
        var passengerA = await RegisterAndLoginPassenger("passengerA");
        var passengerB = await RegisterAndLoginPassenger("passengerB");
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true, seats: 1), driver.AccessToken);

        await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passengerA.AccessToken);

        var response = await Api.CreateBookingRaw(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passengerB.AccessToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [SkippableFact]
    public async Task Passenger_CannotStartDriverRide()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(), driver.AccessToken);

        var response = await Api.StartRide(ride.Id, passenger.AccessToken);

        response.IsSuccessStatusCode.Should().BeFalse();

        var fetched = await Api.GetRide(ride.Id);
        fetched.Status.Should().Be(0); // Still Scheduled
    }

    [SkippableFact]
    public async Task BookingCreated_GeneratesNotificationForDriver()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        await WaitForCondition(async () =>
        {
            var notifications = await Api.GetNotifications(driver.AccessToken);
            return notifications.Any(n => n.BookingId == booking.Id);
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Driver did not receive a notification for the booking within 20 seconds.");

        var driverNotifications = await Api.GetNotifications(driver.AccessToken);
        var notification = driverNotifications.First(n => n.BookingId == booking.Id);
        notification.UserId.Should().Be(driver.UserId);
        notification.RideId.Should().Be(ride.Id);
        notification.IsRead.Should().BeFalse();
    }

    [SkippableFact]
    public async Task BookingConfirmed_GeneratesNotificationForPassenger()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: false), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        var confirmResponse = await Api.ConfirmBooking(booking.Id, driver.AccessToken);
        confirmResponse.EnsureSuccessStatusCode();

        await WaitForCondition(async () =>
        {
            var notifications = await Api.GetNotifications(passenger.AccessToken);
            return notifications.Any(n => n.BookingId == booking.Id && n.Type == 1); // BookingConfirmed
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Passenger did not receive a confirmation notification within 20 seconds.");

        var passengerNotifications = await Api.GetNotifications(passenger.AccessToken);
        var notification = passengerNotifications.First(n => n.BookingId == booking.Id && n.Type == 1);
        notification.UserId.Should().Be(passenger.UserId);
        notification.RideId.Should().Be(ride.Id);
    }

    [SkippableFact]
    public async Task UnreadCount_ReflectsNotifications()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true), driver.AccessToken);

        await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        await WaitForCondition(async () =>
        {
            var count = await Api.GetUnreadCount(driver.AccessToken);
            return count.Count > 0;
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Driver unread count did not increase within 20 seconds.");

        var unread = await Api.GetUnreadCount(driver.AccessToken);
        unread.Count.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task MarkNotificationRead_SetsIsReadTrue()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true), driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        await WaitForCondition(async () =>
        {
            var notifications = await Api.GetNotifications(driver.AccessToken);
            return notifications.Any(n => n.BookingId == booking.Id);
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Driver did not receive a notification within 20 seconds.");

        var notifications = await Api.GetNotifications(driver.AccessToken);
        var notification = notifications.First(n => n.BookingId == booking.Id);
        notification.IsRead.Should().BeFalse();

        var markResponse = await Api.MarkNotificationRead(notification.Id, driver.AccessToken);
        markResponse.EnsureSuccessStatusCode();

        var updated = await Api.GetNotifications(driver.AccessToken);
        updated.First(n => n.Id == notification.Id).IsRead.Should().BeTrue();
    }

    [SkippableFact]
    public async Task MarkAllRead_ClearsUnreadCount()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true), driver.AccessToken);

        await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        await WaitForCondition(async () =>
        {
            var count = await Api.GetUnreadCount(driver.AccessToken);
            return count.Count > 0;
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Driver did not receive any notifications within 20 seconds.");

        var markAllResponse = await Api.MarkAllNotificationsRead(driver.AccessToken);
        markAllResponse.EnsureSuccessStatusCode();

        var afterMarkAll = await Api.GetUnreadCount(driver.AccessToken);
        afterMarkAll.Count.Should().Be(0);
    }

    [SkippableFact]
    public async Task RideCompleted_AutoCompletesConfirmedBookings()
    {
        var driver = await RegisterAndLoginDriver();
        var passenger = await RegisterAndLoginPassenger();
        // Use near-future departure so the ride can be started shortly after creation
        var ride = await Api.CreateRide(
            MakeRideRequest(autoConfirm: true, seats: 3, departureTime: DateTime.UtcNow.AddSeconds(3)),
            driver.AccessToken);

        var booking = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passenger.AccessToken);

        booking.Status.Should().Be(1); // Confirmed (auto-confirm)

        // Wait for departure time to pass so StartRide is allowed
        await Task.Delay(4000);

        // Start then complete the ride
        var startResponse = await Api.StartRide(ride.Id, driver.AccessToken);
        startResponse.EnsureSuccessStatusCode();

        var completeResponse = await Api.CompleteRide(ride.Id, driver.AccessToken);
        completeResponse.EnsureSuccessStatusCode();

        // Wait for RideCompletedEvent to propagate and auto-complete booking
        await WaitForCondition(async () =>
        {
            var bookings = await Api.GetBookingsByRide(ride.Id, driver.AccessToken);
            return bookings.All(b => b.Status == 3); // Completed
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Booking was not auto-completed after ride completion within 20 seconds.");

        var updatedBookings = await Api.GetBookingsByRide(ride.Id, driver.AccessToken);
        updatedBookings.Should().ContainSingle();
        updatedBookings.First().Status.Should().Be(3); // Completed
        updatedBookings.First().CompletedAt.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task RideCancelled_AutoCancelsActiveBookings()
    {
        var driver = await RegisterAndLoginDriver();
        var passengerA = await RegisterAndLoginPassenger("passengerA");
        var passengerB = await RegisterAndLoginPassenger("passengerB");
        var ride = await Api.CreateRide(MakeRideRequest(autoConfirm: true, seats: 4), driver.AccessToken);

        var bookingA = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 1 },
            passengerA.AccessToken);

        var bookingB = await Api.CreateBooking(
            new CreateBookingRequest { RideId = ride.Id, SeatsToBook = 2 },
            passengerB.AccessToken);

        bookingA.Status.Should().Be(1); // Confirmed
        bookingB.Status.Should().Be(1); // Confirmed

        // Cancel the ride
        var cancelResponse = await Api.CancelRide(ride.Id, driver.AccessToken, "Weather conditions");
        cancelResponse.EnsureSuccessStatusCode();

        // Wait for RideCancelledEvent to propagate and auto-cancel bookings
        await WaitForCondition(async () =>
        {
            var bookings = await Api.GetBookingsByRide(ride.Id, driver.AccessToken);
            return bookings.All(b => b.Status == 2); // Cancelled
        },
        timeout: TimeSpan.FromSeconds(20),
        failureMessage: "Bookings were not auto-cancelled after ride cancellation within 20 seconds.");

        var updatedBookings = await Api.GetBookingsByRide(ride.Id, driver.AccessToken);
        updatedBookings.Should().HaveCount(2);
        updatedBookings.Should().AllSatisfy(b =>
        {
            b.Status.Should().Be(2); // Cancelled
            b.CancelledAt.Should().NotBeNull();
            b.CancellationReason.Should().Contain("Weather conditions");
        });
    }

    [SkippableFact]
    public async Task GetNotifications_WithoutToken_Returns401()
    {
        var response = await Api.GetNotificationsRaw("invalid-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
