using Booking.Application.DTOs;
using Booking.Application.Queries.GetBookingsByPassenger;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Interfaces;

namespace Booking.Application.Tests.Queries;

public class GetBookingsByPassengerQueryHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly GetBookingsByPassengerQueryHandler _handler;

    private readonly Guid _passengerId = Guid.NewGuid();

    public GetBookingsByPassengerQueryHandlerTests()
    {
        _bookingRepo = new Mock<IBookingRepository>();
        _handler = new GetBookingsByPassengerQueryHandler(_bookingRepo.Object);
    }

    private BookingEntity CreateBooking(Guid? passengerId = null)
    {
        return BookingEntity.Create(
            rideId: Guid.NewGuid(),
            passengerId: passengerId ?? _passengerId,
            seatsBooked: 1,
            pricePerSeat: 300m,
            currency: "RSD",
            driverId: Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_NoStatusFilter_CallsGetByPassengerId()
    {
        var bookings = new List<BookingEntity> { CreateBooking(), CreateBooking() };
        _bookingRepo
            .Setup(r => r.GetByPassengerId(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookings);

        var query = new GetBookingsByPassengerQuery
        {
            PassengerId = _passengerId,
            Status = null
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.PassengerId.Should().Be(_passengerId));

        _bookingRepo.Verify(
            r => r.GetByPassengerId(_passengerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _bookingRepo.Verify(
            r => r.GetByPassengerAndStatus(It.IsAny<Guid>(), It.IsAny<BookingStatus>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_CallsGetByPassengerAndStatus()
    {
        var booking = CreateBooking();
        booking.Confirm();

        _bookingRepo
            .Setup(r => r.GetByPassengerAndStatus(_passengerId, BookingStatus.Confirmed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity> { booking });

        var query = new GetBookingsByPassengerQuery
        {
            PassengerId = _passengerId,
            Status = BookingStatus.Confirmed
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Status.Should().Be(BookingStatus.Confirmed);

        _bookingRepo.Verify(
            r => r.GetByPassengerAndStatus(_passengerId, BookingStatus.Confirmed, It.IsAny<CancellationToken>()),
            Times.Once);
        _bookingRepo.Verify(
            r => r.GetByPassengerId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NoBookings_ReturnsEmptyList()
    {
        _bookingRepo
            .Setup(r => r.GetByPassengerId(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity>());

        var query = new GetBookingsByPassengerQuery
        {
            PassengerId = _passengerId
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsAllDtoFieldsCorrectly()
    {
        var booking = CreateBooking();
        _bookingRepo
            .Setup(r => r.GetByPassengerId(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingEntity> { booking });

        var query = new GetBookingsByPassengerQuery { PassengerId = _passengerId };

        var result = await _handler.Handle(query, CancellationToken.None);

        var dto = result[0];
        dto.Id.Should().Be(booking.Id);
        dto.RideId.Should().Be(booking.RideId.Value);
        dto.PassengerId.Should().Be(booking.PassengerId.Value);
        dto.DriverId.Should().Be(booking.DriverId);
        dto.SeatsBooked.Should().Be(1);
        dto.TotalPrice.Should().Be(300m);
        dto.Currency.Should().Be("RSD");
        dto.Status.Should().Be(BookingStatus.Pending);
    }
}
