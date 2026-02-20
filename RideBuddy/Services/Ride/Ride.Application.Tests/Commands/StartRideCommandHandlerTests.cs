using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Ride.Application.Commands.StartRide;
using Ride.Application.Interfaces;
using Ride.Domain.Entities;
using Ride.Domain.Enums;
using Ride.Domain.Interfaces;
using SharedKernel;
using Xunit;

namespace Ride.Application.Tests.Commands;

public class StartRideCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<StartRideCommandHandler>> _loggerMock;
    private readonly Mock<IRideRepository> _rideRepositoryMock;
    private readonly StartRideCommandHandler _handler;

    public StartRideCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<StartRideCommandHandler>>();
        _rideRepositoryMock = new Mock<IRideRepository>();

        _unitOfWorkMock.Setup(x => x.Rides).Returns(_rideRepositoryMock.Object);

        _handler = new StartRideCommandHandler(
            _unitOfWorkMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ScheduledRideByDriver_StartsSuccessfully()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var rideId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId, "John", "Doe",
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var command = new StartRideCommand { RideId = rideId, DriverId = driverId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ride.Status.Should().Be(RideStatus.InProgress);
        ride.StartedAt.Should().NotBeNull();
        ride.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _rideRepositoryMock.Verify(x => x.Update(ride, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RideNotFound_ReturnsFailure()
    {
        // Arrange
        var rideId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RideEntity?)null);

        var command = new StartRideCommand { RideId = rideId, DriverId = driverId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");

        _rideRepositoryMock.Verify(x => x.Update(It.IsAny<RideEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongDriver_ReturnsFailure()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var wrongDriverId = Guid.NewGuid();
        var rideId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId, "John", "Doe",
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var command = new StartRideCommand { RideId = rideId, DriverId = wrongDriverId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only the driver can start the ride");
        ride.Status.Should().Be(RideStatus.Scheduled); // Should remain unchanged

        _rideRepositoryMock.Verify(x => x.Update(It.IsAny<RideEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChanges(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Success_PublishesDomainEvents()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var rideId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId, "John", "Doe",
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var command = new StartRideCommand { RideId = rideId, DriverId = driverId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishMany(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Success_ClearsDomainEvents()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var rideId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId, "John", "Doe",
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var command = new StartRideCommand { RideId = rideId, DriverId = driverId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        ride.DomainEvents.Should().BeEmpty();
    }
}
