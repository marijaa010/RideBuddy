using Booking.Application.Commands.CreateBooking;
using Booking.Application.Common;
using Booking.Application.DTOs;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Booking.Application.Tests.Commands;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly Mock<IRideGrpcClient> _rideClient;
    private readonly Mock<IUserGrpcClient> _userClient;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly CreateBookingCommandHandler _handler;

    private readonly Guid _rideId = Guid.NewGuid();
    private readonly Guid _passengerId = Guid.NewGuid();
    private readonly Guid _driverId = Guid.NewGuid();

    public CreateBookingCommandHandlerTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _bookingRepo = new Mock<IBookingRepository>();
        _rideClient = new Mock<IRideGrpcClient>();
        _userClient = new Mock<IUserGrpcClient>();
        _eventPublisher = new Mock<IEventPublisher>();

        _unitOfWork.Setup(u => u.Bookings).Returns(_bookingRepo.Object);

        _handler = new CreateBookingCommandHandler(
            _unitOfWork.Object,
            _rideClient.Object,
            _userClient.Object,
            _eventPublisher.Object,
            Mock.Of<ILogger<CreateBookingCommandHandler>>());
    }

    private CreateBookingCommand CreateCommand(int seats = 2)
    {
        return new CreateBookingCommand
        {
            RideId = _rideId,
            PassengerId = _passengerId,
            SeatsToBook = seats
        };
    }

    private void SetupValidUser()
    {
        _userClient
            .Setup(u => u.ValidateUser(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { UserId = _passengerId, IsValid = true });
    }

    private void SetupValidRide(bool autoConfirm = true, int availableSeats = 4)
    {
        _rideClient
            .Setup(r => r.GetRideInfo(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RideInfoDto
            {
                RideId = _rideId,
                DriverId = _driverId,
                AvailableSeats = availableSeats,
                PricePerSeat = 500m,
                Currency = "RSD",
                IsAvailable = true,
                AutoConfirmBookings = autoConfirm
            });
    }

    private void SetupNoExistingBooking()
    {
        _bookingRepo
            .Setup(r => r.ExistsActiveBooking(_passengerId, _rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupSeatReservation(bool success = true)
    {
        _rideClient
            .Setup(r => r.ReserveSeats(_rideId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success);
    }

    private void SetupFullHappyPath(bool autoConfirm = true)
    {
        SetupValidUser();
        SetupValidRide(autoConfirm: autoConfirm);
        SetupNoExistingBooking();
        SetupSeatReservation(success: true);
    }

    [Fact]
    public async Task Handle_AutoConfirm_ReturnsSuccessWithConfirmedBooking()
    {
        SetupFullHappyPath(autoConfirm: true);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(BookingStatus.Confirmed);
        result.Value.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_AutoConfirm_MapsAllFieldsCorrectly()
    {
        SetupFullHappyPath(autoConfirm: true);

        var result = await _handler.Handle(CreateCommand(seats: 2), CancellationToken.None);

        result.Value.RideId.Should().Be(_rideId);
        result.Value.PassengerId.Should().Be(_passengerId);
        result.Value.DriverId.Should().Be(_driverId);
        result.Value.SeatsBooked.Should().Be(2);
        result.Value.TotalPrice.Should().Be(1000m); // 500 * 2
        result.Value.Currency.Should().Be("RSD");
    }

    [Fact]
    public async Task Handle_ManualApproval_ReturnsSuccessWithPendingBooking()
    {
        SetupFullHappyPath(autoConfirm: false);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BookingStatus.Pending);
        result.Value.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Success_PersistsBookingAndCommitsTransaction()
    {
        SetupFullHappyPath();

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _bookingRepo.Verify(
            r => r.Add(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(
            u => u.SaveChanges(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _unitOfWork.Verify(
            u => u.CommitTransaction(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Success_ReservesSeats()
    {
        SetupFullHappyPath();

        await _handler.Handle(CreateCommand(seats: 2), CancellationToken.None);

        _rideClient.Verify(
            r => r.ReserveSeats(_rideId, 2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Success_PublishesDomainEvents()
    {
        SetupFullHappyPath();

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _eventPublisher.Verify(
            e => e.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNull_ReturnsFailure()
    {
        _userClient
            .Setup(u => u.ValidateUser(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Equals("User not found or is not valid.");
    }

    [Fact]
    public async Task Handle_UserInvalid_ReturnsFailure()
    {
        _userClient
            .Setup(u => u.ValidateUser(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { IsValid = false });

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Equals("User not found or is not valid.");
    }

    [Fact]
    public async Task Handle_RideNull_ReturnsFailure()
    {
        SetupValidUser();
        _rideClient
            .Setup(r => r.GetRideInfo(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RideInfoDto?)null);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Ride not found");
    }

    [Fact]
    public async Task Handle_RideNotAvailable_ReturnsFailure()
    {
        SetupValidUser();
        _rideClient
            .Setup(r => r.GetRideInfo(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RideInfoDto { RideId = _rideId, IsAvailable = false });

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no longer available");
    }

    [Fact]
    public async Task Handle_NotEnoughSeats_ReturnsFailure()
    {
        SetupValidUser();
        SetupValidRide(availableSeats: 1);

        var result = await _handler.Handle(CreateCommand(seats: 3), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Not enough available seats");
    }

    [Fact]
    public async Task Handle_DuplicateActiveBooking_ReturnsFailure()
    {
        SetupValidUser();
        SetupValidRide();
        _bookingRepo
            .Setup(r => r.ExistsActiveBooking(_passengerId, _rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already have an active booking");
    }

    [Fact]
    public async Task Handle_SeatReservationFails_RejectsBookingAndReturnsFailure()
    {
        SetupValidUser();
        SetupValidRide();
        SetupNoExistingBooking();
        SetupSeatReservation(success: false);

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Could not reserve seats");

        _bookingRepo.Verify(
            r => r.Update(
                It.Is<BookingEntity>(b => b.Status == BookingStatus.Rejected),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.CommitTransaction(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExceptionAfterSeatReservation_RollsBackAndReleasesSeats()
    {
        SetupValidUser();
        SetupValidRide();
        SetupNoExistingBooking();
        SetupSeatReservation(success: true);

        _bookingRepo
            .Setup(r => r.Update(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB connection lost"));

        var result = await _handler.Handle(CreateCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _unitOfWork.Verify(u => u.RollbackTransaction(It.IsAny<CancellationToken>()), Times.Once);
        _rideClient.Verify(
            r => r.ReleaseSeats(_rideId, 2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidUser_DoesNotCallRideService()
    {
        _userClient
            .Setup(u => u.ValidateUser(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _rideClient.Verify(
            r => r.GetRideInfo(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RideNotFound_DoesNotCheckExistingBooking()
    {
        SetupValidUser();
        _rideClient
            .Setup(r => r.GetRideInfo(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RideInfoDto?)null);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _bookingRepo.Verify(
            r => r.ExistsActiveBooking(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateBooking_DoesNotReserveSeats()
    {
        SetupValidUser();
        SetupValidRide();
        _bookingRepo
            .Setup(r => r.ExistsActiveBooking(_passengerId, _rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.Handle(CreateCommand(), CancellationToken.None);

        _rideClient.Verify(
            r => r.ReserveSeats(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
