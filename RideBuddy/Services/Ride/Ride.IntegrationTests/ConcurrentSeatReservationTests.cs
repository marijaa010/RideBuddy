using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ride.Domain.Entities;
using Ride.Domain.Exceptions;
using Ride.Infrastructure.Persistence;
using Xunit;

namespace Ride.IntegrationTests;

/// <summary>
/// Integration tests for concurrent seat reservation scenarios.
/// Tests optimistic concurrency control to prevent overbooking.
/// </summary>
public class ConcurrentSeatReservationTests : IDisposable
{
    private readonly RideDbContext _dbContext;
    private readonly string _databaseName;

    public ConcurrentSeatReservationTests()
    {
        _databaseName = $"ConcurrencyTestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<RideDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        _dbContext = new RideDbContext(options);
    }

    [Fact]
    public async Task ConcurrentReservation_OneSucceedsOneFails_PreventsOverbooking()
    {
        // Arrange: Create a ride with only 1 seat available
        var ride = RideEntity.Create(
            driverId: Guid.NewGuid(),
            driverFirstName: "John",
            driverLastName: "Doe",
            originName: "Belgrade",
            originLat: 44.7866,
            originLng: 20.4489,
            destinationName: "Novi Sad",
            destLat: 45.2671,
            destLng: 19.8335,
            departureTime: DateTime.UtcNow.AddHours(2),
            availableSeats: 1,
            pricePerSeat: 500m,
            currency: "RSD");

        _dbContext.Rides.Add(ride);
        await _dbContext.SaveChangesAsync();

        var rideId = ride.Id;

        // Create two separate contexts simulating two concurrent requests
        var options = new DbContextOptionsBuilder<RideDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        var context1 = new RideDbContext(options);
        var context2 = new RideDbContext(options);

        try
        {
            // Act: Simulate two users trying to book the last seat simultaneously
            var ride1 = await context1.Rides.FindAsync(rideId);
            var ride2 = await context2.Rides.FindAsync(rideId);

            // Both see 1 available seat
            ride1!.AvailableSeats.Value.Should().Be(1);
            ride2!.AvailableSeats.Value.Should().Be(1);

            // Both try to reserve 1 seat
            ride1.ReserveSeats(1);
            ride2.ReserveSeats(1);

            // First one saves successfully
            await context1.SaveChangesAsync();

            // Second one should fail due to concurrency conflict
            var act = async () => await context2.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

            // Assert: Verify only one booking succeeded
            _dbContext.ChangeTracker.Clear();
            var finalRide = await _dbContext.Rides.FindAsync(rideId);
            finalRide!.AvailableSeats.Value.Should().Be(0); // Only 1 seat reserved, not 2
        }
        finally
        {
            await context1.DisposeAsync();
            await context2.DisposeAsync();
        }
    }

    [Fact]
    public async Task SequentialReservations_WorkCorrectly()
    {
        // Arrange
        var ride = RideEntity.Create(
            Guid.NewGuid(), "Jane", "Smith",
            "Nis", 0, 0, "Belgrade", 0, 0,
            DateTime.UtcNow.AddHours(3),
            5, 700m, "RSD");

        _dbContext.Rides.Add(ride);
        await _dbContext.SaveChangesAsync();

        // Act: Sequential reservations
        _dbContext.ChangeTracker.Clear();
        var ride1 = await _dbContext.Rides.FindAsync(ride.Id);
        ride1!.ReserveSeats(2);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
        var ride2 = await _dbContext.Rides.FindAsync(ride.Id);
        ride2!.ReserveSeats(2);
        await _dbContext.SaveChangesAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();
        var finalRide = await _dbContext.Rides.FindAsync(ride.Id);
        finalRide!.AvailableSeats.Value.Should().Be(1); // 5 - 2 - 2 = 1
        finalRide.TotalSeats.Value.Should().Be(5);
    }

    [Fact]
    public async Task ConcurrentReservation_BothSucceed_WhenEnoughSeatsAvailable()
    {
        // Arrange: Create a ride with 4 seats
        var ride = RideEntity.Create(
            Guid.NewGuid(), "Bob", "Johnson",
            "A", 0, 0, "B", 0, 0,
            DateTime.UtcNow.AddHours(1),
            4, 300m, "RSD");

        _dbContext.Rides.Add(ride);
        await _dbContext.SaveChangesAsync();

        var rideId = ride.Id;

        var options = new DbContextOptionsBuilder<RideDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        var context1 = new RideDbContext(options);
        var context2 = new RideDbContext(options);

        try
        {
            // Act: Two users trying to book 2 seats each (4 total available)
            var ride1 = await context1.Rides.FindAsync(rideId);
            var ride2 = await context2.Rides.FindAsync(rideId);

            ride1!.ReserveSeats(2);
            ride2!.ReserveSeats(2);

            // First booking succeeds
            await context1.SaveChangesAsync();

            // Second booking will fail with concurrency exception
            var act = async () => await context2.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

            // The second user needs to retry with fresh data
            _dbContext.ChangeTracker.Clear();
            var ride2Retry = await _dbContext.Rides.FindAsync(rideId);
            ride2Retry!.AvailableSeats.Value.Should().Be(2); // Now only 2 seats available
            ride2Retry.ReserveSeats(2);
            await _dbContext.SaveChangesAsync();

            // Assert: All 4 seats reserved
            _dbContext.ChangeTracker.Clear();
            var finalRide = await _dbContext.Rides.FindAsync(rideId);
            finalRide!.AvailableSeats.Value.Should().Be(0);
        }
        finally
        {
            await context1.DisposeAsync();
            await context2.DisposeAsync();
        }
    }

    [Fact]
    public async Task ReserveAndRelease_WorksCorrectly()
    {
        // Arrange
        var ride = RideEntity.Create(
            Guid.NewGuid(), "Test", "Driver",
            "X", 0, 0, "Y", 0, 0,
            DateTime.UtcNow.AddHours(5),
            3, 500m, "RSD");

        _dbContext.Rides.Add(ride);
        await _dbContext.SaveChangesAsync();

        // Act: Reserve then release seats
        _dbContext.ChangeTracker.Clear();
        var rideForReserve = await _dbContext.Rides.FindAsync(ride.Id);
        rideForReserve!.ReserveSeats(3);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
        var rideForRelease = await _dbContext.Rides.FindAsync(ride.Id);
        rideForRelease!.ReleaseSeats(2);
        await _dbContext.SaveChangesAsync();

        // Assert
        _dbContext.ChangeTracker.Clear();
        var finalRide = await _dbContext.Rides.FindAsync(ride.Id);
        finalRide!.AvailableSeats.Value.Should().Be(2); // Reserved 3, released 2 = 1 used, 2 available
        finalRide.TotalSeats.Value.Should().Be(3);
    }

    [Fact]
    public async Task ReserveMoreThanAvailable_ThrowsException()
    {
        // Arrange
        var ride = RideEntity.Create(
            Guid.NewGuid(), "Test", "Driver",
            "A", 0, 0, "B", 0, 0,
            DateTime.UtcNow.AddHours(1),
            2, 400m, "RSD");

        _dbContext.Rides.Add(ride);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        _dbContext.ChangeTracker.Clear();
        var rideToBook = await _dbContext.Rides.FindAsync(ride.Id);
        var act = () => rideToBook!.ReserveSeats(3);

        act.Should().Throw<RideDomainException>()
            .WithMessage("*Not enough*");
    }

    [Fact]
    public async Task VersionIncrementsOnEachUpdate()
    {
        // Arrange
        var ride = RideEntity.Create(
            Guid.NewGuid(), "Test", "Driver",
            "A", 0, 0, "B", 0, 0,
            DateTime.UtcNow.AddHours(1),
            5, 500m, "RSD");

        _dbContext.Rides.Add(ride);
        await _dbContext.SaveChangesAsync();

        var initialVersion = ride.Version;

        // Act: Update multiple times
        _dbContext.ChangeTracker.Clear();
        var ride1 = await _dbContext.Rides.FindAsync(ride.Id);
        ride1!.ReserveSeats(1);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
        var ride2 = await _dbContext.Rides.FindAsync(ride.Id);
        ride2!.ReleaseSeats(1);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
        var ride3 = await _dbContext.Rides.FindAsync(ride.Id);
        ride3!.ReserveSeats(2);
        await _dbContext.SaveChangesAsync();

        // Assert: Version incremented 3 times
        _dbContext.ChangeTracker.Clear();
        var finalRide = await _dbContext.Rides.FindAsync(ride.Id);
        finalRide!.Version.Should().Be(initialVersion + 3);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
