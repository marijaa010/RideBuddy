using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Ride.Application.Commands.CancelRide;
using Ride.Application.Commands.CompleteRide;
using Ride.Application.Commands.CreateRide;
using Ride.Application.Commands.StartRide;
using Ride.Application.DTOs;
using Ride.Application.Interfaces;
using Ride.Application.Queries.GetRideById;
using Ride.Domain.Enums;
using Ride.Domain.Interfaces;
using Ride.Infrastructure.Persistence;
using Ride.Infrastructure.Repositories;
using SharedKernel;
using Xunit;

namespace Ride.IntegrationTests;

/// <summary>
/// Integration tests for the complete ride lifecycle: Create → Start → Complete → Cancel
/// Uses in-memory database to test full command/query flow with persistence.
/// </summary>
public class RideLifecycleIntegrationTests : IDisposable
{
    private readonly RideDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IUserGrpcClient> _userClientMock;

    public RideLifecycleIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<RideDbContext>()
            .UseInMemoryDatabase(databaseName: $"RideTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new RideDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);

        _eventPublisherMock = new Mock<IEventPublisher>();
        _userClientMock = new Mock<IUserGrpcClient>();

        // Setup default user validation response
        _userClientMock
            .Setup(x => x.ValidateUser(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto
            {
                IsValid = true,
                FirstName = "John",
                LastName = "Doe"
            });
    }

    [Fact]
    public async Task FullLifecycle_CreateStartComplete_WorksCorrectly()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var createHandler = new CreateRideCommandHandler(
            _unitOfWork,
            _userClientMock.Object,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CreateRideCommandHandler>>());

        var createCommand = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Belgrade",
            OriginLatitude = 44.7866,
            OriginLongitude = 20.4489,
            DestinationName = "Novi Sad",
            DestinationLatitude = 45.2671,
            DestinationLongitude = 19.8335,
            DepartureTime = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 4,
            PricePerSeat = 500m,
            Currency = "RSD",
            AutoConfirmBookings = true
        };

        // Act 1: Create ride
        var createResult = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert: Ride created successfully
        createResult.IsSuccess.Should().BeTrue();
        var rideId = createResult.Value.Id;

        // Verify persisted to database
        var persistedRide = await _dbContext.Rides.FindAsync(rideId);
        persistedRide.Should().NotBeNull();
        persistedRide!.Status.Should().Be(RideStatus.Scheduled);
        persistedRide.AvailableSeats.Value.Should().Be(4);

        // Act 2: Start ride
        var startHandler = new StartRideCommandHandler(
            _unitOfWork,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<StartRideCommandHandler>>());

        var startCommand = new StartRideCommand { RideId = rideId, DriverId = driverId };
        var startResult = await startHandler.Handle(startCommand, CancellationToken.None);

        // Assert: Ride started
        startResult.IsSuccess.Should().BeTrue();

        _dbContext.ChangeTracker.Clear();
        var startedRide = await _dbContext.Rides.FindAsync(rideId);
        startedRide!.Status.Should().Be(RideStatus.InProgress);
        startedRide.StartedAt.Should().NotBeNull();

        // Act 3: Complete ride
        var completeHandler = new CompleteRideCommandHandler(
            _unitOfWork,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CompleteRideCommandHandler>>());

        var completeCommand = new CompleteRideCommand { RideId = rideId, DriverId = driverId };
        var completeResult = await completeHandler.Handle(completeCommand, CancellationToken.None);

        // Assert: Ride completed
        completeResult.IsSuccess.Should().BeTrue();

        _dbContext.ChangeTracker.Clear();
        var completedRide = await _dbContext.Rides.FindAsync(rideId);
        completedRide!.Status.Should().Be(RideStatus.Completed);
        completedRide.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FullLifecycle_CreateThenCancel_WorksCorrectly()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var createHandler = new CreateRideCommandHandler(
            _unitOfWork,
            _userClientMock.Object,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CreateRideCommandHandler>>());

        var createCommand = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Nis",
            OriginLatitude = 43.3209,
            OriginLongitude = 21.8958,
            DestinationName = "Belgrade",
            DestinationLatitude = 44.7866,
            DestinationLongitude = 20.4489,
            DepartureTime = DateTime.UtcNow.AddHours(3),
            AvailableSeats = 3,
            PricePerSeat = 700m,
            Currency = "RSD",
            AutoConfirmBookings = false
        };

        // Act 1: Create ride
        var createResult = await createHandler.Handle(createCommand, CancellationToken.None);
        var rideId = createResult.Value.Id;

        // Act 2: Cancel ride
        var cancelHandler = new CancelRideCommandHandler(
            _unitOfWork,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CancelRideCommandHandler>>());

        var cancelCommand = new CancelRideCommand
        {
            RideId = rideId,
            DriverId = driverId,
            Reason = "Driver unavailable due to emergency"
        };

        var cancelResult = await cancelHandler.Handle(cancelCommand, CancellationToken.None);

        // Assert: Ride cancelled
        cancelResult.IsSuccess.Should().BeTrue();

        _dbContext.ChangeTracker.Clear();
        var cancelledRide = await _dbContext.Rides.FindAsync(rideId);
        cancelledRide!.Status.Should().Be(RideStatus.Cancelled);
        cancelledRide.CancelledAt.Should().NotBeNull();
        cancelledRide.CancellationReason.Should().Be("Driver unavailable due to emergency");
    }

    [Fact]
    public async Task QueryAfterCreate_ReturnsPersistedRide()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var createHandler = new CreateRideCommandHandler(
            _unitOfWork,
            _userClientMock.Object,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CreateRideCommandHandler>>());

        var createCommand = new CreateRideCommand
        {
            DriverId = driverId,
            OriginName = "Kragujevac",
            OriginLatitude = 44.0128,
            OriginLongitude = 20.9114,
            DestinationName = "Belgrade",
            DestinationLatitude = 44.7866,
            DestinationLongitude = 20.4489,
            DepartureTime = DateTime.UtcNow.AddHours(5),
            AvailableSeats = 2,
            PricePerSeat = 400m,
            Currency = "RSD",
            AutoConfirmBookings = true
        };

        // Act: Create and query
        var createResult = await createHandler.Handle(createCommand, CancellationToken.None);
        var rideId = createResult.Value.Id;

        _dbContext.ChangeTracker.Clear();

        var queryHandler = new GetRideByIdQueryHandler(_unitOfWork);
        var query = new GetRideByIdQuery { RideId = rideId };
        var queriedRide = await queryHandler.Handle(query, CancellationToken.None);

        // Assert: Queried ride matches created ride
        queriedRide.Should().NotBeNull();
        queriedRide!.Id.Should().Be(rideId);
        queriedRide.DriverId.Should().Be(driverId);
        queriedRide.OriginName.Should().Be("Kragujevac");
        queriedRide.DestinationName.Should().Be("Belgrade");
        queriedRide.AvailableSeats.Should().Be(2);
        queriedRide.TotalSeats.Should().Be(2);
        queriedRide.PricePerSeat.Should().Be(400m);
        queriedRide.Currency.Should().Be("RSD");
        queriedRide.AutoConfirmBookings.Should().BeTrue();
        queriedRide.Status.Should().Be(RideStatus.Scheduled);
    }

    [Fact]
    public async Task MultipleRides_PersistAndQueryCorrectly()
    {
        // Arrange
        var driver1Id = Guid.NewGuid();
        var driver2Id = Guid.NewGuid();

        _userClientMock
            .Setup(x => x.ValidateUser(driver1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { IsValid = true, FirstName = "Alice", LastName = "Smith" });

        _userClientMock
            .Setup(x => x.ValidateUser(driver2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfoDto { IsValid = true, FirstName = "Bob", LastName = "Jones" });

        var createHandler = new CreateRideCommandHandler(
            _unitOfWork,
            _userClientMock.Object,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CreateRideCommandHandler>>());

        // Act: Create multiple rides
        var ride1Command = new CreateRideCommand
        {
            DriverId = driver1Id,
            OriginName = "A",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationName = "B",
            DestinationLatitude = 1,
            DestinationLongitude = 1,
            DepartureTime = DateTime.UtcNow.AddHours(1),
            AvailableSeats = 3,
            PricePerSeat = 300m,
            Currency = "RSD"
        };

        var ride2Command = new CreateRideCommand
        {
            DriverId = driver2Id,
            OriginName = "C",
            OriginLatitude = 2,
            OriginLongitude = 2,
            DestinationName = "D",
            DestinationLatitude = 3,
            DestinationLongitude = 3,
            DepartureTime = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 5,
            PricePerSeat = 600m,
            Currency = "EUR"
        };

        var result1 = await createHandler.Handle(ride1Command, CancellationToken.None);
        var result2 = await createHandler.Handle(ride2Command, CancellationToken.None);

        // Assert: Both rides persisted correctly
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var allRides = await _dbContext.Rides.ToListAsync();
        allRides.Should().HaveCount(2);

        var persistedRide1 = allRides.First(r => r.Id == result1.Value.Id);
        var persistedRide2 = allRides.First(r => r.Id == result2.Value.Id);

        persistedRide1.DriverFirstName.Should().Be("Alice");
        persistedRide1.DriverLastName.Should().Be("Smith");
        persistedRide1.Origin.Name.Should().Be("A");
        persistedRide1.PricePerSeat.Amount.Should().Be(300m);
        persistedRide1.PricePerSeat.Currency.Should().Be("RSD");

        persistedRide2.DriverFirstName.Should().Be("Bob");
        persistedRide2.DriverLastName.Should().Be("Jones");
        persistedRide2.Origin.Name.Should().Be("C");
        persistedRide2.PricePerSeat.Amount.Should().Be(600m);
        persistedRide2.PricePerSeat.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task InvalidDriver_CreateRide_ReturnsFailure()
    {
        // Arrange
        var invalidDriverId = Guid.NewGuid();

        _userClientMock
            .Setup(x => x.ValidateUser(invalidDriverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        var createHandler = new CreateRideCommandHandler(
            _unitOfWork,
            _userClientMock.Object,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<CreateRideCommandHandler>>());

        var createCommand = new CreateRideCommand
        {
            DriverId = invalidDriverId,
            OriginName = "A",
            OriginLatitude = 0,
            OriginLongitude = 0,
            DestinationName = "B",
            DestinationLatitude = 1,
            DestinationLongitude = 1,
            DepartureTime = DateTime.UtcNow.AddHours(1),
            AvailableSeats = 3,
            PricePerSeat = 300m,
            Currency = "RSD"
        };

        // Act
        var result = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Driver not found");

        // Verify no ride was persisted
        var rides = await _dbContext.Rides.ToListAsync();
        rides.Should().BeEmpty();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
