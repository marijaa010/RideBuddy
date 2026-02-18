using Booking.Application.Commands.RejectBooking;
using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Booking.Application.Tests.Commands;

public class RejectBookingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly Mock<IRideGrpcClient> _rideClient;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly RejectBookingCommandHandler _handler;

    private readonly Guid _driverId = Guid.NewGuid();

    public RejectBookingCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _bookingRepo = new Mock<IBookingRepository>();
        _rideClient = new Mock<IRideGrpcClient>();
        _eventPublisher = new Mock<IEventPublisher>();

        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepo.Object);

        _handler = new RejectBookingCommandHandler(
            _unitOfWork.Object,
            _rideClient.Object,
            _eventPublisher.Object,
            Mock.Of<ILogger<RejectBookingCommandHandler>>());
    }

    private BookingEntity CreatePendingBooking()
    {
        return BookingEntity.Create(
            rideId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            passengerFirstName: "John",
            passengerLastName: "Doe",
            seatsBooked: 2,
            pricePerSeat: 500m,
            currency: "RSD",
            driverId: _driverId);
    }

    private void SetupSeatRelease(bool success = true)
    {
        _rideClient
            .Setup(r => r.ReleaseSeats(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success);
    }

    [Fact]
    public async Task Handle_DriverRejectsPending_ReturnsSuccessAndReleasesSeats()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = _driverId,
            Reason = "Too much luggage"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.CancellationReason.Should().Be("Too much luggage");
        booking.RejectedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_EmptyReason_UsesDefaultMessage()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = _driverId,
            Reason = ""
        };

        await _handler.Handle(command, CancellationToken.None);

        booking.CancellationReason.Should().Be("Rejected by driver");
    }

    [Fact]
    public async Task Handle_Success_CommitsTransactionAndPublishesEvents()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease();

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = _driverId
        };

        await _handler.Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _rideClient.Verify(
            r => r.ReleaseSeats(booking.RideId.Value, 2, It.IsAny<CancellationToken>()),
            Times.Once);
        _eventPublisher.Verify(
            e => e.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SeatReleaseFails_StillSucceeds()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        SetupSeatRelease(success: false);

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = _driverId
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

        var command = new RejectBookingCommand
        {
            BookingId = bookingId,
            DriverId = _driverId
        };

        await Assert.ThrowsAsync<BookingNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongUser_ReturnsFailure()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = Guid.NewGuid()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only the driver");
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_ReturnsFailureAndRollsBack()
    {
        var booking = CreatePendingBooking();
        booking.Confirm(); // no longer Pending

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _unitOfWork.Verify(u => u.RollbackTransaction(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DbException_RollsBack()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        _bookingRepo
            .Setup(r => r.Update(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var command = new RejectBookingCommand
        {
            BookingId = booking.Id,
            DriverId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _unitOfWork.Verify(u => u.RollbackTransaction(It.IsAny<CancellationToken>()), Times.Once);
    }
}
