using Booking.Application.Commands.CompleteBooking;
using Booking.Application.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using Booking.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Booking.Application.Tests.Commands;

public class CompleteBookingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly CompleteBookingCommandHandler _handler;

    private readonly Guid _driverId = Guid.NewGuid();

    public CompleteBookingCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _bookingRepo = new Mock<IBookingRepository>();
        _eventPublisher = new Mock<IEventPublisher>();

        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepo.Object);

        _handler = new CompleteBookingCommandHandler(
            _unitOfWork.Object,
            _eventPublisher.Object,
            Mock.Of<ILogger<CompleteBookingCommandHandler>>());
    }

    private BookingEntity CreateConfirmedBooking()
    {
        var booking = BookingEntity.Create(
            rideId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            seatsBooked: 2,
            pricePerSeat: 500m,
            currency: "RSD",
            driverId: _driverId);
        booking.Confirm();
        booking.ClearDomainEvents();
        return booking;
    }

    [Fact]
    public async Task Handle_DriverCompletesConfirmed_ReturnsSuccess()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CompleteBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Completed);
        booking.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Success_PersistsAndPublishesEvents()
    {
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CompleteBookingCommand
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

        var command = new CompleteBookingCommand
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
        var booking = CreateConfirmedBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CompleteBookingCommand
        {
            BookingId = booking.Id,
            UserId = Guid.NewGuid()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Equals("Only the driver can complete a booking.");
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_PendingBooking_ReturnsFailure()
    {
        var booking = BookingEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), 2, 500m, "RSD", _driverId);

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CompleteBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlreadyCompleted_ReturnsFailure()
    {
        var booking = CreateConfirmedBooking();
        booking.Complete();
        booking.ClearDomainEvents();

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CompleteBookingCommand
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
        var booking = CreateConfirmedBooking();
        booking.Cancel("test");
        booking.ClearDomainEvents();

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var command = new CompleteBookingCommand
        {
            BookingId = booking.Id,
            UserId = _driverId
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
