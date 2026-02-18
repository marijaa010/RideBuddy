using Booking.Application.Commands.ConfirmBooking;
using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Booking.Application.Tests.Commands;

public class ConfirmBookingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly ConfirmBookingCommandHandler _handler;

    private readonly Guid _driverId = Guid.NewGuid();

    public ConfirmBookingCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _bookingRepo = new Mock<IBookingRepository>();
        _eventPublisher = new Mock<IEventPublisher>();

        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepo.Object);

        _handler = new ConfirmBookingCommandHandler(
            _unitOfWork.Object,
            _eventPublisher.Object,
            Mock.Of<ILogger<ConfirmBookingCommandHandler>>());
    }

    private BookingEntity CreatePendingBooking(Guid? driverId = null)
    {
        return BookingEntity.Create(
            rideId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            passengerFirstName: "John",
            passengerLastName: "Doe",
            seatsBooked: 2,
            pricePerSeat: 500m,
            currency: "RSD",
            driverId: driverId ?? _driverId);
    }

    [Fact]
    public async Task Handle_DriverConfirmsPending_ReturnsSuccess()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Success_PersistsAndPublishesEvents()
    {
        var booking = CreatePendingBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        await _handler.Handle(command, CancellationToken.None);

        _bookingRepo.Verify(r => r.Update(booking, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisher.Verify(
            e => e.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsBookingNotFoundException()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepo
            .Setup(r => r.GetById(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var command = new ConfirmBookingCommand
        {
            BookingId = bookingId,
            UserId = _driverId
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

        var command = new ConfirmBookingCommand
        {
            BookingId = booking.Id,
            UserId = Guid.NewGuid()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only the driver");
        booking.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_ReturnsFailure()
    {
        var booking = CreatePendingBooking();
        booking.Confirm();

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_CancelledBooking_ReturnsFailure()
    {
        var booking = CreatePendingBooking();
        booking.Cancel("test");

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new ConfirmBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
