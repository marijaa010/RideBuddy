using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Exceptions;
using FluentAssertions;

namespace Booking.Domain.Tests.Entities;

public class BookingEntityTests
{
    private readonly Guid _rideId = Guid.NewGuid();
    private readonly Guid _passengerId = Guid.NewGuid();
    private readonly Guid _driverId = Guid.NewGuid();
    private const int Seats = 2;
    private const decimal PricePerSeat = 500m;
    private const string Currency = "RSD";

    private BookingEntity CreateBooking()
    {
        return BookingEntity.Create(_rideId, _passengerId, Seats, PricePerSeat, Currency,_driverId);
    }

    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        var booking = CreateBooking();

        booking.Id.Should().NotBe(Guid.Empty);
        booking.RideId.Value.Should().Be(_rideId);
        booking.PassengerId.Value.Should().Be(_passengerId);
        booking.DriverId.Should().Be(_driverId);
        booking.SeatsBooked.Value.Should().Be(Seats);
        booking.TotalPrice.Amount.Should().Be(PricePerSeat * Seats);
        booking.TotalPrice.Currency.Should().Be(Currency);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.BookedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        booking.ConfirmedAt.Should().BeNull();
        booking.CancelledAt.Should().BeNull();
        booking.CompletedAt.Should().BeNull();
        booking.CancellationReason.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesBookingCreatedEvent()
    {
        var booking = CreateBooking();

        booking.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BookingCreatedEvent>();

        var @event = (BookingCreatedEvent)booking.DomainEvents.First();
        @event.BookingId.Should().Be(booking.Id);
        @event.RideId.Should().Be(_rideId);
        @event.PassengerId.Should().Be(_passengerId);
        @event.SeatsBooked.Should().Be(Seats);
        @event.TotalPrice.Should().Be(PricePerSeat * Seats);
        @event.Currency.Should().Be(Currency);
    }

    [Fact]
    public void Create_VersionStartsAtZero()
    {
        var booking = CreateBooking();

        booking.Version.Should().Be(0);
    }

    [Fact]
    public void Confirm_FromPending_SetsStatusToConfirmed()
    {
        var booking = CreateBooking();

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAt.Should().NotBeNull();
        booking.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Confirm_IncrementsVersion()
    {
        var booking = CreateBooking();

        booking.Confirm();

        booking.Version.Should().Be(1);
    }

    [Fact]
    public void Confirm_RaisesBookingConfirmedEvent()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.Confirm();

        booking.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BookingConfirmedEvent>();

        var @event = (BookingConfirmedEvent)booking.DomainEvents.First();
        @event.BookingId.Should().Be(booking.Id);
        @event.RideId.Should().Be(_rideId);
        @event.PassengerId.Should().Be(_passengerId);
        @event.SeatsBooked.Should().Be(Seats);
        @event.TotalPrice.Should().Be(PricePerSeat * Seats);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Rejected)]
    public void Confirm_FromNonPendingStatus_ThrowsException(BookingStatus status)
    {
        var booking = CreateBooking();
        MoveToStatus(booking, status);
        var expectedMessage = $"Booking cannot be confirmed because it is in '{status}' status.";

        var act = () => booking.Confirm();

        act.Should().Throw<BookingDomainException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void Cancel_FromPending_SetsStatusToCancelled()
    {
        var booking = CreateBooking();
        const string reason = "Changed my mind";

        booking.Cancel(reason);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancelledAt.Should().NotBeNull();
        booking.CancellationReason.Should().Be(reason);
    }

    [Fact]
    public void Cancel_FromConfirmed_SetsStatusToCancelled()
    {
        var booking = CreateBooking();
        booking.Confirm();
        const string reason = "Emergency";

        booking.Cancel(reason);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be(reason);
    }

    [Fact]
    public void Cancel_IncrementsVersion()
    {
        var booking = CreateBooking();
        var versionBefore = booking.Version;

        booking.Cancel("Reason");

        booking.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Cancel_RaisesBookingCancelledEvent()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();
        const string reason = "No longer needed";

        booking.Cancel(reason);

        booking.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BookingCancelledEvent>();

        var @event = (BookingCancelledEvent)booking.DomainEvents.First();
        @event.BookingId.Should().Be(booking.Id);
        @event.SeatsReleased.Should().Be(Seats);
        @event.CancellationReason.Should().Be(reason);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsException()
    {
        var booking = CreateBooking();
        booking.Cancel("First cancellation");

        var act = () => booking.Cancel("Second cancellation");

        act.Should().Throw<BookingDomainException>()
            .WithMessage("Booking is already cancelled.");
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsException()
    {
        var booking = CreateBooking();
        booking.Confirm();
        booking.Complete();

        var act = () => booking.Cancel("Too late");

        act.Should().Throw<BookingDomainException>()
            .WithMessage("Cannot cancel a completed ride.");
    }

    [Fact]
    public void Reject_FromPending_SetsStatusToRejected()
    {
        var booking = CreateBooking();
        const string reason = "Driver unavailable";

        booking.Reject(reason);

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.CancellationReason.Should().Be(reason);
    }

    [Fact]
    public void Reject_IncrementsVersion()
    {
        var booking = CreateBooking();

        booking.Reject("Reason");

        booking.Version.Should().Be(1);
    }

    [Fact]
    public void Reject_DoesNotRaiseDomainEvent()
    {
        var booking = CreateBooking();
        booking.ClearDomainEvents();

        booking.Reject("No seats");

        booking.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Rejected)]
    public void Reject_FromNonPendingStatus_ThrowsException(BookingStatus status)
    {
        var booking = CreateBooking();
        var expectedMessage = $"Only bookings in 'Pending' status can be rejected. Current status: '{status}'.";
        MoveToStatus(booking, status);

        var act = () => booking.Reject("Reason");

        act.Should().Throw<BookingDomainException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void Complete_FromConfirmed_SetsStatusToCompleted()
    {
        var booking = CreateBooking();
        booking.Confirm();

        booking.Complete();

        booking.Status.Should().Be(BookingStatus.Completed);
        booking.CompletedAt.Should().NotBeNull();
        booking.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_IncrementsVersion()
    {
        var booking = CreateBooking();
        booking.Confirm();
        var versionBefore = booking.Version;

        booking.Complete();

        booking.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Complete_RaisesBookingCompletedEvent()
    {
        var booking = CreateBooking();
        booking.Confirm();
        booking.ClearDomainEvents();

        booking.Complete();

        booking.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BookingCompletedEvent>();

        var @event = (BookingCompletedEvent)booking.DomainEvents.First();
        @event.BookingId.Should().Be(booking.Id);
        @event.RideId.Should().Be(_rideId);
        @event.PassengerId.Should().Be(_passengerId);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Rejected)]
    public void Complete_FromNonConfirmedStatus_ThrowsException(BookingStatus status)
    {
        var booking = CreateBooking();
        var expectedMessage = $"Only confirmed bookings can be marked as completed. Current status: '{status}'.";
        MoveToStatus(booking, status);

        var act = () => booking.Complete();

        act.Should().Throw<BookingDomainException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void CanBeCancelled_WhenPending_ReturnsTrue()
    {
        var booking = CreateBooking();

        booking.CanBeCancelled().Should().BeTrue();
    }

    [Fact]
    public void CanBeCancelled_WhenConfirmed_ReturnsTrue()
    {
        var booking = CreateBooking();
        booking.Confirm();

        booking.CanBeCancelled().Should().BeTrue();
    }

    [Fact]
    public void CanBeCancelled_WhenCancelled_ReturnsFalse()
    {
        var booking = CreateBooking();
        booking.Cancel("Reason");

        booking.CanBeCancelled().Should().BeFalse();
    }

    [Fact]
    public void CanBeCancelled_WhenCompleted_ReturnsFalse()
    {
        var booking = CreateBooking();
        booking.Confirm();
        booking.Complete();

        booking.CanBeCancelled().Should().BeFalse();
    }

    [Fact]
    public void CanBeCancelled_WhenRejected_ReturnsFalse()
    {
        var booking = CreateBooking();
        booking.Reject("Reason");

        booking.CanBeCancelled().Should().BeFalse();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var booking = CreateBooking();
        booking.DomainEvents.Should().NotBeEmpty();

        booking.ClearDomainEvents();

        booking.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void FullLifecycle_Pending_Confirmed_Completed()
    {
        var booking = CreateBooking();
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Version.Should().Be(0);

        booking.Confirm();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.Version.Should().Be(1);

        booking.Complete();
        booking.Status.Should().Be(BookingStatus.Completed);
        booking.Version.Should().Be(2);
    }

    [Fact]
    public void FullLifecycle_Pending_Confirmed_Cancelled()
    {
        var booking = CreateBooking();

        booking.Confirm();
        booking.Cancel("Driver cancelled");

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.Version.Should().Be(2);
    }

    [Fact]
    public void FullLifecycle_AccumulatesDomainEvents()
    {
        var booking = CreateBooking();
        booking.Confirm();

        booking.DomainEvents.Should().HaveCount(2);
        booking.DomainEvents.First().Should().BeOfType<BookingCreatedEvent>();
        booking.DomainEvents.Last().Should().BeOfType<BookingConfirmedEvent>();
    }

    private void MoveToStatus(BookingEntity booking, BookingStatus status)
    {
        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm();
                break;
            case BookingStatus.Cancelled:
                booking.Cancel("Test cancellation");
                break;
            case BookingStatus.Completed:
                booking.Confirm();
                booking.Complete();
                break;
            case BookingStatus.Rejected:
                booking.Reject("Test rejection");
                break;
            case BookingStatus.Pending:
                break;
        }
    }
}
