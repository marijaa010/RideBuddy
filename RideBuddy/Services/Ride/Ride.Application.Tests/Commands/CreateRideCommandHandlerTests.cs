using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ride.Application.Commands.CreateRide;
using Ride.Application.DTOs;
using Ride.Application.Interfaces;
using Ride.Domain.Entities;
using Ride.Domain.Enums;
using Ride.Domain.Interfaces;
using SharedKernel;
using Xunit;

namespace Ride.Application.Tests.Commands;

public class CreateRideCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserGrpcClient> _userGrpcClientMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<CreateRideCommandHandler>> _loggerMock;
    private readonly CreateRideCommandHandler _handler;

    public CreateRideCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userGrpcClientMock = new Mock<IUserGrpcClient>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<CreateRideCommandHandler>>();

        _handler = new CreateRideCommandHandler(
            _unitOfWorkMock.Object,
            _userGrpcClientMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDriver_CreatesRideSuccessfully()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var command = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 3,
            PricePerSeat = 500,
            Currency = "RSD",
            AutoConfirmBookings = true
        };

        _userGrpcClientMock
            .Setup(x => x.ValidateUser(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { UserId = driverId, IsValid = true });

        var rideRepositoryMock = new Mock<IRideRepository>();
        _unitOfWorkMock.Setup(x => x.Rides).Returns(rideRepositoryMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DriverId.Should().Be(driverId);
        result.Value.OriginName.Should().Be("Belgrade");
        result.Value.DestinationName.Should().Be("Novi Sad");
        result.Value.AvailableSeats.Should().Be(3);
        result.Value.PricePerSeat.Should().Be(500);
        result.Value.Status.Should().Be(RideStatus.Scheduled);

        rideRepositoryMock.Verify(x => x.Add(It.IsAny<RideEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LocalDateTime_ConvertsToUtc()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var localTime = new DateTime(2026, 3, 15, 14, 0, 0, DateTimeKind.Local);

        var command = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = localTime,
            AvailableSeats = 3,
            PricePerSeat = 500,
            Currency = "RSD"
        };

        _userGrpcClientMock
            .Setup(x => x.ValidateUser(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { UserId = driverId, IsValid = true });

        var rideRepositoryMock = new Mock<IRideRepository>();
        _unitOfWorkMock.Setup(x => x.Rides).Returns(rideRepositoryMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DepartureTime.Kind.Should().Be(DateTimeKind.Utc);

        rideRepositoryMock.Verify(x => x.Add(
            It.Is<RideEntity>(r => r.DepartureTime.Kind == DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidDriver_ReturnsFailure()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var command = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 3,
            PricePerSeat = 500,
            Currency = "RSD"
        };

        _userGrpcClientMock
            .Setup(x => x.ValidateUser(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { UserId = driverId, IsValid = false });

        var rideRepositoryMock = new Mock<IRideRepository>();
        _unitOfWorkMock.Setup(x => x.Rides).Returns(rideRepositoryMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Driver not found or is not valid");

        rideRepositoryMock.Verify(x => x.Add(It.IsAny<RideEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullUserInfo_ReturnsFailure()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var command = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 3,
            PricePerSeat = 500,
            Currency = "RSD"
        };

        _userGrpcClientMock
            .Setup(x => x.ValidateUser(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Driver not found or is not valid");
    }

    [Fact]
    public async Task Handle_Success_PublishesDomainEvents()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var command = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 3,
            PricePerSeat = 500,
            Currency = "RSD"
        };

        _userGrpcClientMock
            .Setup(x => x.ValidateUser(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { UserId = driverId, IsValid = true });

        var rideRepositoryMock = new Mock<IRideRepository>();
        _unitOfWorkMock.Setup(x => x.Rides).Returns(rideRepositoryMock.Object);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_MapsAllFieldsCorrectly()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var departureTime = DateTime.UtcNow.AddHours(5);

        var command = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Nis",
            OriginLatitude = 43.3209,
            OriginLongitude = 21.8958,
            DestinationName = "Sofia",
            DestinationLatitude = 42.6977,
            DestinationLongitude = 23.3219,
            DepartureTime = departureTime,
            AvailableSeats = 2,
            PricePerSeat = 1500,
            Currency = "RSD",
            AutoConfirmBookings = false
        };

        _userGrpcClientMock
            .Setup(x => x.ValidateUser(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { UserId = driverId, IsValid = true });

        var rideRepositoryMock = new Mock<IRideRepository>();
        _unitOfWorkMock.Setup(x => x.Rides).Returns(rideRepositoryMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        dto.DriverId.Should().Be(driverId);
        dto.OriginName.Should().Be("Nis");
        dto.OriginLatitude.Should().Be(43.3209);
        dto.OriginLongitude.Should().Be(21.8958);
        dto.DestinationName.Should().Be("Sofia");
        dto.DestinationLatitude.Should().Be(42.6977);
        dto.DestinationLongitude.Should().Be(23.3219);
        dto.TotalSeats.Should().Be(2);
        dto.AvailableSeats.Should().Be(2);
        dto.PricePerSeat.Should().Be(1500);
        dto.Currency.Should().Be("RSD");
        dto.AutoConfirmBookings.Should().BeFalse();
        dto.Status.Should().Be(RideStatus.Scheduled);
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
