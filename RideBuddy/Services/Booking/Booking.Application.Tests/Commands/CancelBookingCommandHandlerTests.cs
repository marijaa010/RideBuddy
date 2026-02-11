using Booking.Application.Commands.CancelBooking;
using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Booking.Application.Tests.Commands;

public class CancelBookingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly Mock<IRideGrpcClient> _rideClient;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly CancelBookingCommandHandler _handler;

    private readonly Guid _passengerId = Guid.NewGuid();
    private readonly Guid _driverId = Guid.NewGuid();

    public CancelBookingCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _bookingRepo = new Mock<IBookingRepository>();
        _rideClient = new Mock<IRideGrpcClient>();
        _eventPublisher = new Mock<IEventPublisher>();

        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepo.Object);

        _handler = new CancelBookingCommandHandler(
            _unitOfWork.Object,
            _rideClient.Object,
            _eventPublisher.Object,
            Mock.Of<ILogger<CancelBookingCommandHandler>>());
    }

    private BookingEntity CreatePendingBooking()
    {
        var booking = BookingEntity.Create(
            rideId: Guid.NewGuid(),
            passengerId: _passengerId,
            seatsBooked: 2,
            pricePerSeat: 500m,
            currency: "RSD",
            driverId: _driverId);
        booking.ClearDomainEvents();
        return booking;
    }

    private BookingEntity CreateConfirmedBooking()
    {
        var booking = CreatePendingBooking();
        booking.Confirm();
        booking.ClearDomainEvents();
        return booking;
    }

    private void SetupSeatRelease(bool success = true)
    {
        _rideClient
            .Setup(r => r.ReleaseSeats(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success);
    }

    [Fact]
    public async Task Handle_PassengerCancelsConfirmed_ReturnsSuccess()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId,
            Reason = "Changed my plans"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Changed my plans");
        booking.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_DriverCancels_ReturnsSuccess()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId,
            Reason = "Ride cancelled"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.CancellationReason.Should().Be("Ride cancelled");
        booking.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_PendingBooking_CanBeCancelled()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_EmptyReason_UsesDefaultMessage()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId,
            Reason = ""
        };

        await _handler.Handle(command, CancellationToken.None);

        booking.CancellationReason.Should().Be("Cancelled by passenger");
    }

    [Fact]
    public async Task Handle_Success_ReleasesSeatsCommitsAndPublishes()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId
        };

        await _handler.Handle(command, CancellationToken.None);

        _rideClient.Verify(
            r => r.ReleaseSeats(booking.RideId.Value, 2, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(
            e => e.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SeatReleaseFails_StillCommits()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease(success: false);

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsBookingNotFoundException()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepo
            .Setup(r => r.GetById(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var command = new CancelBookingCommand
        {
            BookingId = bookingId,
            UserId = _passengerId
        };

        await Assert.ThrowsAsync<BookingNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ReturnsFailure()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = Guid.NewGuid()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Equals("You do not have permission to cancel this booking.");
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_CompletedBooking_CannotBeCancelled()
    {
        var booking = CreateConfirmedBooking();
        booking.Complete();
        booking.ClearDomainEvents();

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Equals($"Booking in '{booking.Status}' status cannot be cancelled.");
    }

    [Fact]
    public async Task Handle_RejectedBooking_CannotBeCancelled()
    {
        var booking = CreatePendingBooking();
        booking.Reject("test");
        booking.ClearDomainEvents();

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Equals($"Booking in '{booking.Status}' status cannot be cancelled.");
    }

    [Fact]
    public async Task Handle_DbException_RollsBack()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        _bookingRepo
            .Setup(r => r.Update(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = new CancelBookingCommand
        {
            BookingId = booking.Id,
            UserId = _passengerId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _unitOfWork.Verify(u => u.RollbackTransaction(It.IsAny<CancellationToken>()), Times.Once);
    }
}
