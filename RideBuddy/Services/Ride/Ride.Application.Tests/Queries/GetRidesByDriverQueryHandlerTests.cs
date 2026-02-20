using FluentAssertions;
using Moq;
using Ride.Application.Queries.GetRidesByDriver;
using Ride.Domain.Entities;
using Ride.Domain.Interfaces;
using Xunit;

namespace Ride.Application.Tests.Queries;

public class GetRidesByDriverQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRideRepository> _rideRepositoryMock;
    private readonly GetRidesByDriverQueryHandler _handler;

    public GetRidesByDriverQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rideRepositoryMock = new Mock<IRideRepository>();

        _unitOfWorkMock.Setup(x => x.Rides).Returns(_rideRepositoryMock.Object);

        _handler = new GetRidesByDriverQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_DriverHasRides_ReturnsAllRidesForDriver()
    {
        // Arrange
        var driverId = Guid.NewGuid();

        var ride1 = RideEntity.Create(
            driverId, "John", "Doe",
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        var ride2 = RideEntity.Create(
            driverId, "John", "Doe",
            "Nis", 43.3209, 21.8958,
            "Belgrade", 44.7866, 20.4489,
            DateTime.UtcNow.AddHours(5),
            2, 700m, "RSD", false);

        var rides = new List<RideEntity> { ride1, ride2 };

        _rideRepositoryMock
            .Setup(x => x.GetByDriverId(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rides);

        var query = new GetRidesByDriverQuery { DriverId = driverId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        result[0].DriverId.Should().Be(driverId);
        result[0].OriginName.Should().Be("Belgrade");
        result[0].DestinationName.Should().Be("Novi Sad");
        result[0].PricePerSeat.Should().Be(500);

        result[1].DriverId.Should().Be(driverId);
        result[1].OriginName.Should().Be("Nis");
        result[1].DestinationName.Should().Be("Belgrade");
        result[1].PricePerSeat.Should().Be(700);
    }

    [Fact]
    public async Task Handle_DriverHasNoRides_ReturnsEmptyList()
    {
        // Arrange
        var driverId = Guid.NewGuid();

        _rideRepositoryMock
            .Setup(x => x.GetByDriverId(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RideEntity>());

        var query = new GetRidesByDriverQuery { DriverId = driverId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsRidesOrderedByDepartureTime()
    {
        // Arrange
        var driverId = Guid.NewGuid();

        var futureTime1 = DateTime.UtcNow.AddHours(5);
        var futureTime2 = DateTime.UtcNow.AddHours(2);
        var futureTime3 = DateTime.UtcNow.AddHours(8);

        var ride1 = RideEntity.Create(
            driverId, "John", "Doe",
            "A", 0, 0, "B", 0, 0,
            futureTime1, 3, 500m, "RSD", true);

        var ride2 = RideEntity.Create(
            driverId, "John", "Doe",
            "C", 0, 0, "D", 0, 0,
            futureTime2, 2, 700m, "RSD", false);

        var ride3 = RideEntity.Create(
            driverId, "John", "Doe",
            "E", 0, 0, "F", 0, 0,
            futureTime3, 4, 300m, "RSD", true);

        // Repository returns rides sorted by departure time DESCENDING
        var rides = new List<RideEntity> { ride3, ride1, ride2 }; // 8hrs, 5hrs, 2hrs

        _rideRepositoryMock
            .Setup(x => x.GetByDriverId(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rides);

        var query = new GetRidesByDriverQuery { DriverId = driverId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - should be ordered by departure time DESCENDING (latest first)
        result.Should().HaveCount(3);

        // Verify the rides are in the correct order (latest to earliest) by checking destination names
        // This avoids DateTime timezone comparison issues
        result[0].DestinationName.Should().Be("F"); // ride3 with futureTime3 (8 hours) - latest
        result[1].DestinationName.Should().Be("B"); // ride1 with futureTime1 (5 hours) - middle
        result[2].DestinationName.Should().Be("D"); // ride2 with futureTime2 (2 hours) - earliest
    }

    [Fact]
    public async Task Handle_IncludesAllStatuses()
    {
        // Arrange
        var driverId = Guid.NewGuid();

        var scheduledRide = RideEntity.Create(
            driverId, "John", "Doe",
            "A", 0, 0, "B", 0, 0,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        var inProgressRide = RideEntity.Create(
            driverId, "John", "Doe",
            "C", 0, 0, "D", 0, 0,
            DateTime.UtcNow.AddHours(3),
            2, 700m, "RSD", false);
        inProgressRide.Start();

        var completedRide = RideEntity.Create(
            driverId, "John", "Doe",
            "E", 0, 0, "F", 0, 0,
            DateTime.UtcNow.AddHours(4),
            4, 300m, "RSD", true);
        completedRide.Start();
        completedRide.Complete();

        var cancelledRide = RideEntity.Create(
            driverId, "John", "Doe",
            "G", 0, 0, "H", 0, 0,
            DateTime.UtcNow.AddHours(5),
            1, 600m, "RSD", true);
        cancelledRide.Cancel("Driver unavailable");

        var rides = new List<RideEntity> { scheduledRide, inProgressRide, completedRide, cancelledRide };

        _rideRepositoryMock
            .Setup(x => x.GetByDriverId(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rides);

        var query = new GetRidesByDriverQuery { DriverId = driverId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - all rides should be included regardless of status
        result.Should().HaveCount(4);
        result.Should().Contain(r => r.Status == Domain.Enums.RideStatus.Scheduled);
        result.Should().Contain(r => r.Status == Domain.Enums.RideStatus.InProgress);
        result.Should().Contain(r => r.Status == Domain.Enums.RideStatus.Completed);
        result.Should().Contain(r => r.Status == Domain.Enums.RideStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_MapsAllFieldsCorrectly()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var departureTime = DateTime.UtcNow.AddHours(3);

        var ride = RideEntity.Create(
            driverId, "Jane", "Smith",
            "Novi Sad", 45.2671, 19.8335,
            "Belgrade", 44.7866, 20.4489,
            departureTime,
            5, 1200m, "EUR", false);

        _rideRepositoryMock
            .Setup(x => x.GetByDriverId(driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RideEntity> { ride });

        var query = new GetRidesByDriverQuery { DriverId = driverId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var dto = result[0];

        dto.DriverId.Should().Be(driverId);
        dto.DriverName.Should().Be("Jane Smith");
        dto.OriginName.Should().Be("Novi Sad");
        dto.OriginLatitude.Should().Be(45.2671);
        dto.OriginLongitude.Should().Be(19.8335);
        dto.DestinationName.Should().Be("Belgrade");
        dto.DestinationLatitude.Should().Be(44.7866);
        dto.DestinationLongitude.Should().Be(20.4489);
        dto.DepartureTime.Should().Be(departureTime);
        dto.TotalSeats.Should().Be(5);
        dto.AvailableSeats.Should().Be(5);
        dto.PricePerSeat.Should().Be(1200m);
        dto.Currency.Should().Be("EUR");
        dto.AutoConfirmBookings.Should().BeFalse();
        dto.Status.Should().Be(Domain.Enums.RideStatus.Scheduled);
    }
}
