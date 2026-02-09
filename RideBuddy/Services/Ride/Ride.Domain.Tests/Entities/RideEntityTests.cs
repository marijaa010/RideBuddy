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
            Guid.NewGuid(), "A", 0, 0, "B", 0, 0,
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
}
