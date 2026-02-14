using FluentAssertions;
using Moq;
using Ride.Application.Queries.GetRideById;
using Ride.Domain.Entities;
using Ride.Domain.Enums;
using Ride.Domain.Interfaces;
using Xunit;

namespace Ride.Application.Tests.Queries;

public class GetRideByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRideRepository> _rideRepositoryMock;
    private readonly GetRideByIdQueryHandler _handler;

    public GetRideByIdQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rideRepositoryMock = new Mock<IRideRepository>();

        _unitOfWorkMock.Setup(x => x.Rides).Returns(_rideRepositoryMock.Object);

        _handler = new GetRideByIdQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_RideExists_ReturnsMappedDto()
    {
        // Arrange
        var rideId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId,
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500, "RSD", true);

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var query = new GetRideByIdQuery { RideId = rideId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.DriverId.Should().Be(driverId);
        result.OriginName.Should().Be("Belgrade");
        result.OriginLatitude.Should().Be(44.7866);
        result.OriginLongitude.Should().Be(20.4489);
        result.DestinationName.Should().Be("Novi Sad");
        result.DestinationLatitude.Should().Be(45.2671);
        result.DestinationLongitude.Should().Be(19.8335);
        result.AvailableSeats.Should().Be(3);
        result.PricePerSeat.Should().Be(500);
        result.Currency.Should().Be("RSD");
        result.Status.Should().Be(RideStatus.Scheduled);
        result.AutoConfirmBookings.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RideNotFound_ReturnsNull()
    {
        // Arrange
        var rideId = Guid.NewGuid();

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RideEntity?)null);

        var query = new GetRideByIdQuery { RideId = rideId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CancelledRide_MapsAllFieldsCorrectly()
    {
        // Arrange
        var rideId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId,
            "Nis", 43.3209, 21.8958,
            "Sofia", 42.6977, 23.3219,
            DateTime.UtcNow.AddHours(5),
            2, 1500, "RSD", false);

        ride.Cancel("Weather conditions");

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var query = new GetRideByIdQuery { RideId = rideId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(RideStatus.Cancelled);
        result.CancellationReason.Should().Be("Weather conditions");
        result.CancelledAt.Should().NotBeNull();
        result.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_CompletedRide_MapsTimestampsCorrectly()
    {
        // Arrange
        var rideId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var ride = RideEntity.Create(
            driverId,
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500, "RSD", true);

        ride.Start();
        ride.Complete();

        _rideRepositoryMock
            .Setup(x => x.GetById(rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ride);

        var query = new GetRideByIdQuery { RideId = rideId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(RideStatus.Completed);
        result.StartedAt.Should().NotBeNull();
        result.CompletedAt.Should().NotBeNull();
        result.StartedAt.Should().BeBefore(result.CompletedAt.Value);
    }
}
