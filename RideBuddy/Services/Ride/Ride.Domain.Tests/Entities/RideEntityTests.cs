using Ride.Domain.Entities;
using Ride.Domain.Enums;
using Ride.Domain.Events;
using Ride.Domain.Exceptions;

namespace Ride.Domain.Tests.Entities;

public class RideEntityTests
{
    private static RideEntity CreateValidRide(int seats = 4, bool autoConfirm = true)
    {
        return RideEntity.Create(
            driverId: Guid.NewGuid(),
            driverFirstName: "John",
            driverLastName: "Doe",
            originName: "Novi Sad",
            originLat: 45.2671,
            originLng: 19.8335,
            destinationName: "Beograd",
            destLat: 44.7866,
            destLng: 20.4489,
            departureTime: DateTime.UtcNow.AddHours(2),
            availableSeats: seats,
            pricePerSeat: 500m,
            currency: "RSD",
            autoConfirmBookings: autoConfirm);
    }

    [Fact]
    public void Create_ValidData_CreatesRideWithScheduledStatus()
    {
        var ride = CreateValidRide();

        ride.Status.Should().Be(RideStatus.Scheduled);
        ride.AvailableSeats.Value.Should().Be(4);
        ride.TotalSeats.Value.Should().Be(4);
        ride.Origin.Name.Should().Be("Novi Sad");
        ride.Destination.Name.Should().Be("Beograd");
        ride.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RideCreatedEvent>();
    }

    [Fact]
    public void Create_PastDepartureTime_ThrowsDomainException()
    {
        var act = () => RideEntity.Create(
            Guid.NewGuid(), "John", "Doe", "Belgrade", 0, 0, "Novi Sad", 0, 0,
            DateTime.UtcNow.AddHours(-1), 4, 500m, "RSD");

        act.Should().Throw<RideDomainException>()
            .WithMessage("*future*");
    }

    [Fact]
    public void ReserveSeats_ValidCount_DecreasesAvailableSeats()
    {
        var ride = CreateValidRide(seats: 4);
        ride.ClearDomainEvents();

        ride.ReserveSeats(2);

        ride.AvailableSeats.Value.Should().Be(2);
        ride.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SeatsReservedEvent>();
    }

    [Fact]
    public void ReserveSeats_MoreThanAvailable_ThrowsDomainException()
    {
        var ride = CreateValidRide(seats: 2);

        var act = () => ride.ReserveSeats(3);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Not enough*");
    }

    [Fact]
    public void ReleaseSeats_ValidCount_IncreasesAvailableSeats()
    {
        var ride = CreateValidRide(seats: 4);
        ride.ReserveSeats(2);
        ride.ClearDomainEvents();

        ride.ReleaseSeats(1);

        ride.AvailableSeats.Value.Should().Be(3);
        ride.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SeatsReleasedEvent>();
    }

    [Fact]
    public void ReleaseSeats_ExceedsTotal_ThrowsDomainException()
    {
        var ride = CreateValidRide(seats: 4);

        var act = () => ride.ReleaseSeats(1);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Cannot release more*");
    }

    [Fact]
    public void Start_ScheduledRide_TransitionsToInProgress()
    {
        var ride = CreateValidRide();
        ride.ClearDomainEvents();

        ride.Start();

        ride.Status.Should().Be(RideStatus.InProgress);
        ride.StartedAt.Should().NotBeNull();
        ride.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RideStartedEvent>();
    }

    [Fact]
    public void Complete_InProgressRide_TransitionsToCompleted()
    {
        var ride = CreateValidRide();
        ride.Start();
        ride.ClearDomainEvents();

        ride.Complete();

        ride.Status.Should().Be(RideStatus.Completed);
        ride.CompletedAt.Should().NotBeNull();
        ride.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RideCompletedEvent>();
    }

    [Fact]
    public void Complete_ScheduledRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();

        var act = () => ride.Complete();

        act.Should().Throw<RideDomainException>()
            .WithMessage("*in-progress*");
    }

    [Fact]
    public void Cancel_ScheduledRide_TransitionsToCancelled()
    {
        var ride = CreateValidRide();
        ride.ClearDomainEvents();

        ride.Cancel("Driver unavailable");

        ride.Status.Should().Be(RideStatus.Cancelled);
        ride.CancellationReason.Should().Be("Driver unavailable");
        ride.CancelledAt.Should().NotBeNull();
        ride.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RideCancelledEvent>();
    }

    [Fact]
    public void Cancel_CompletedRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();
        ride.Start();
        ride.Complete();

        var act = () => ride.Cancel("reason");

        act.Should().Throw<RideDomainException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void Cancel_AlreadyCancelledRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();
        ride.Cancel("first cancel");

        var act = () => ride.Cancel("second cancel");

        act.Should().Throw<RideDomainException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public void IsAvailable_ScheduledWithSeats_ReturnsTrue()
    {
        var ride = CreateValidRide();

        ride.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_CancelledRide_ReturnsFalse()
    {
        var ride = CreateValidRide();
        ride.Cancel("cancelled");

        ride.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_NoAvailableSeats_ReturnsFalse()
    {
        var ride = CreateValidRide(seats: 2);
        ride.ReserveSeats(2);

        ride.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_InProgressRide_ReturnsFalse()
    {
        var ride = CreateValidRide();
        ride.Start();

        ride.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void ReserveSeats_ExactlyAllSeats_LeavesZeroAvailable()
    {
        var ride = CreateValidRide(seats: 3);

        ride.ReserveSeats(3);

        ride.AvailableSeats.Value.Should().Be(0);
        ride.TotalSeats.Value.Should().Be(3);
    }

    [Fact]
    public void ReserveSeats_OnCancelledRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();
        ride.Cancel("Cancelled");

        var act = () => ride.ReserveSeats(1);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Cannot reserve seats*");
    }

    [Fact]
    public void ReserveSeats_ZeroSeats_ThrowsDomainException()
    {
        var ride = CreateValidRide();

        var act = () => ride.ReserveSeats(0);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*greater than 0*");
    }

    [Fact]
    public void ReserveSeats_NegativeSeats_ThrowsDomainException()
    {
        var ride = CreateValidRide();

        var act = () => ride.ReserveSeats(-1);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*greater than 0*");
    }

    [Fact]
    public void ReleaseSeats_AfterReservation_CorrectlyUpdatesSeats()
    {
        var ride = CreateValidRide(seats: 5);
        ride.ReserveSeats(3);

        ride.ReleaseSeats(2);

        ride.AvailableSeats.Value.Should().Be(4);
        ride.TotalSeats.Value.Should().Be(5);
    }

    [Fact]
    public void ReleaseSeats_ZeroSeats_ThrowsDomainException()
    {
        var ride = CreateValidRide(seats: 4);
        ride.ReserveSeats(2);

        var act = () => ride.ReleaseSeats(0);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*greater than 0*");
    }

    [Fact]
    public void ReleaseSeats_NegativeSeats_ThrowsDomainException()
    {
        var ride = CreateValidRide(seats: 4);
        ride.ReserveSeats(2);

        var act = () => ride.ReleaseSeats(-1);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*greater than 0*");
    }

    [Fact]
    public void Start_AlreadyStartedRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();
        ride.Start();

        var act = () => ride.Start();

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Only scheduled rides*");
    }

    [Fact]
    public void Start_CancelledRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();
        ride.Cancel("Cancelled");

        var act = () => ride.Start();

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Only scheduled rides*");
    }

    [Fact]
    public void Complete_ScheduledRideNotYetStarted_ThrowsDomainException()
    {
        var ride = CreateValidRide();

        var act = () => ride.Complete();

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Only in-progress*");
    }

    [Fact]
    public void Complete_AlreadyCompletedRide_ThrowsDomainException()
    {
        var ride = CreateValidRide();
        ride.Start();
        ride.Complete();

        var act = () => ride.Complete();

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Only in-progress*");
    }

    [Fact]
    public void Create_MaxSeats_CreatesSuccessfully()
    {
        var ride = RideEntity.Create(
            Guid.NewGuid(), "John", "Doe",
            "Belgrade", 0, 0, "Novi Sad", 0, 0,
            DateTime.UtcNow.AddHours(2), 8, 500m, "RSD");

        ride.AvailableSeats.Value.Should().Be(8);
        ride.TotalSeats.Value.Should().Be(8);
    }

    [Fact]
    public void Create_ZeroPrice_CreatesSuccessfully()
    {
        var ride = RideEntity.Create(
            Guid.NewGuid(), "John", "Doe",
            "Belgrade", 0, 0, "Novi Sad", 0, 0,
            DateTime.UtcNow.AddHours(2), 4, 0m, "RSD");

        ride.PricePerSeat.Amount.Should().Be(0m);
    }

    [Fact]
    public void Version_IncreasesOnStateChange()
    {
        var ride = CreateValidRide(seats: 4);
        var initialVersion = ride.Version;

        ride.ReserveSeats(2);

        ride.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void Version_IncreasesOnMultipleChanges()
    {
        var ride = CreateValidRide(seats: 4);
        var initialVersion = ride.Version;

        ride.ReserveSeats(2);
        ride.ReleaseSeats(1);
        ride.ReserveSeats(1);

        ride.Version.Should().Be(initialVersion + 3);
    }

    [Fact]
    public void FullLifecycle_ScheduledToInProgressToCompleted_WorksCorrectly()
    {
        var ride = CreateValidRide();

        // Scheduled
        ride.Status.Should().Be(RideStatus.Scheduled);

        // Start
        ride.Start();
        ride.Status.Should().Be(RideStatus.InProgress);
        ride.StartedAt.Should().NotBeNull();

        // Complete
        ride.Complete();
        ride.Status.Should().Be(RideStatus.Completed);
        ride.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullLifecycle_ScheduledToCancelled_WorksCorrectly()
    {
        var ride = CreateValidRide(seats: 4);
        ride.ReserveSeats(2);

        ride.Cancel("Driver sick");

        ride.Status.Should().Be(RideStatus.Cancelled);
        ride.CancelledAt.Should().NotBeNull();
        ride.CancellationReason.Should().Be("Driver sick");
        // Seats remain as they were when cancelled
        ride.AvailableSeats.Value.Should().Be(2);
    }

    [Fact]
    public void AutoConfirmBookings_IsPersisted()
    {
        var rideWithAutoConfirm = CreateValidRide(autoConfirm: true);
        var rideWithoutAutoConfirm = CreateValidRide(autoConfirm: false);

        rideWithAutoConfirm.AutoConfirmBookings.Should().BeTrue();
        rideWithoutAutoConfirm.AutoConfirmBookings.Should().BeFalse();
    }
}
