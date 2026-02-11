using Booking.Application.DTOs;
using Booking.Application.Queries.GetBookingsByRide;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Interfaces;

namespace Booking.Application.Tests.Queries;

public class GetBookingsByRideQueryHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly GetBookingsByRideQueryHandler _handler;

    private readonly Guid _rideId = Guid.NewGuid();

    public GetBookingsByRideQueryHandlerTests()
    {
        _bookingRepo = new Mock<IBookingRepository>();
        _handler = new GetBookingsByRideQueryHandler(_bookingRepo.Object);
    }

    private BookingEntity CreateBookingForRide()
    {
        return BookingEntity.Create(
            rideId: _rideId,
            passengerId: Guid.NewGuid(),
            seatsBooked: 1,
            pricePerSeat: 500m,
            currency: "RSD",
            driverId: Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_RideHasBookings_ReturnsMappedDtos()
    {
        var bookings = new List<BookingEntity>
        {
            CreateBookingForRide(),
            CreateBookingForRide(),
            CreateBookingForRide()
        };

        _bookingRepo
            .Setup(r => r.GetByRideId(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);

        var query = new GetBookingsByRideQuery { RideId = _rideId };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(b => b.RideId.Should().Be(_rideId));
    }

    [Fact]
    public async Task Handle_NoBookingsForRide_ReturnsEmptyList()
    {
        _bookingRepo
            .Setup(r => r.GetByRideId(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>());

        var query = new GetBookingsByRideQuery { RideId = _rideId };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MixedStatuses_ReturnsAll()
    {
        var pending = CreateBookingForRide();

        var confirmed = CreateBookingForRide();
        confirmed.Confirm();

        var cancelled = CreateBookingForRide();
        cancelled.Cancel("test");

        _bookingRepo
            .Setup(r => r.GetByRideId(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity> { pending, confirmed, cancelled });

        var query = new GetBookingsByRideQuery { RideId = _rideId };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(b => b.Status).Should().Contain(BookingStatus.Pending);
        result.Select(b => b.Status).Should().Contain(BookingStatus.Confirmed);
        result.Select(b => b.Status).Should().Contain(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_MapsAllDtoFieldsCorrectly()
    {
        var booking = CreateBookingForRide();
        _bookingRepo
            .Setup(r => r.GetByRideId(_rideId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity> { booking });

        var query = new GetBookingsByRideQuery { RideId = _rideId };

        var result = await _handler.Handle(query, CancellationToken.None);

        var dto = result[0];
        dto.Id.Should().Be(booking.Id);
        dto.RideId.Should().Be(_rideId);
        dto.PassengerId.Should().Be(booking.PassengerId.Value);
        dto.DriverId.Should().Be(booking.DriverId);
        dto.SeatsBooked.Should().Be(1);
        dto.TotalPrice.Should().Be(500m);
        dto.Currency.Should().Be("RSD");
    }
}
