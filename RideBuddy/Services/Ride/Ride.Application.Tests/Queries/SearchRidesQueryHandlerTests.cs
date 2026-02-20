using FluentAssertions;
using Moq;
using Ride.Application.Queries.SearchRides;
using Ride.Domain.Entities;
using Ride.Domain.Interfaces;
using Xunit;

namespace Ride.Application.Tests.Queries;

public class SearchRidesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRideRepository> _rideRepositoryMock;
    private readonly SearchRidesQueryHandler _handler;

    public SearchRidesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rideRepositoryMock = new Mock<IRideRepository>();

        _unitOfWorkMock.Setup(x => x.Rides).Returns(_rideRepositoryMock.Object);

        _handler = new SearchRidesQueryHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_RidesFound_ReturnsMappedDtos()
    {
        // Arrange
        var driverId1 = Guid.NewGuid();
        var driverId2 = Guid.NewGuid();

        var ride1 = RideEntity.Create(
            driverId1, "John", "Doe",
            "Belgrade", 44.7866, 20.4489,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(2),
            3, 500m, "RSD", true);

        var ride2 = RideEntity.Create(
            driverId2, "John", "Doe",
            "Nis", 43.3209, 21.8958,
            "Novi Sad", 45.2671, 19.8335,
            DateTime.UtcNow.AddHours(4),
            2, 700m, "RSD", false);

        var rides = new List<RideEntity> { ride1, ride2 };

        _rideRepositoryMock
            .Setup(x => x.Search(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rides);

        var query = new SearchRidesQuery
        {
            Origin = "Belgrade",
            Destination = "Novi Sad",
            Date = DateTime.UtcNow.Date,
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        result[0].DriverId.Should().Be(driverId1);
        result[0].OriginName.Should().Be("Belgrade");
        result[0].DestinationName.Should().Be("Novi Sad");
        result[0].AvailableSeats.Should().Be(3);
        result[0].PricePerSeat.Should().Be(500);

        result[1].DriverId.Should().Be(driverId2);
        result[1].OriginName.Should().Be("Nis");
        result[1].AvailableSeats.Should().Be(2);
        result[1].PricePerSeat.Should().Be(700);
    }

    [Fact]
    public async Task Handle_NoRidesFound_ReturnsEmptyList()
    {
        // Arrange
        _rideRepositoryMock
            .Setup(x => x.Search(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RideEntity>());

        var query = new SearchRidesQuery
        {
            Origin = "Belgrade",
            Destination = "Paris",
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithDateFilter_PassesCorrectParameters()
    {
        // Arrange
        var date = new DateTime(2026, 3, 15);

        _rideRepositoryMock
            .Setup(x => x.Search(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RideEntity>());

        var query = new SearchRidesQuery
        {
            Origin = "Belgrade",
            Destination = "Novi Sad",
            Date = date,
            Page = 1,
            PageSize = 20
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _rideRepositoryMock.Verify(
            x => x.Search(
                "Belgrade",
                "Novi Sad",
                date,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullFilters_PassesNullToRepository()
    {
        // Arrange
        _rideRepositoryMock
            .Setup(x => x.Search(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RideEntity>());

        var query = new SearchRidesQuery
        {
            Origin = null,
            Destination = null,
            Date = null,
            Page = 1,
            PageSize = 20
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _rideRepositoryMock.Verify(
            x => x.Search(
                null,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CustomPaging_PassesCorrectParameters()
    {
        // Arrange
        _rideRepositoryMock
            .Setup(x => x.Search(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RideEntity>());

        var query = new SearchRidesQuery
        {
            Page = 3,
            PageSize = 50
        };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _rideRepositoryMock.Verify(
            x => x.Search(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                3,
                50,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
