using Booking.Application.DTOs;
using Booking.Application.Queries.GetBookingById;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Interfaces;

namespace Booking.Application.Tests.Queries;

public class GetBookingByIdQueryHandlerTests
{
    private readonly Mock<IBookingRepository> _bookingRepo;
    private readonly GetBookingByIdQueryHandler _handler;

    public GetBookingByIdQueryHandlerTests()
    {
        _bookingRepo = new Mock<IBookingRepository>();
        _handler = new GetBookingByIdQueryHandler(_bookingRepo.Object);
    }

    private BookingEntity CreateBooking()
    {
        return BookingEntity.Create(
            rideId: Guid.NewGuid(),
            passengerId: Guid.NewGuid(),
            passengerFirstName: "John",
            passengerLastName: "Doe",
            seatsBooked: 2,
            pricePerSeat: 500m,
            currency: "RSD",
            driverId: Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_BookingExists_ReturnsMappedDto()
    {
        var booking = CreateBooking();
        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var query = new GetBookingByIdQuery { BookingId = booking.Id };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.RideId.Should().Be(booking.RideId.Value);
        result.PassengerId.Should().Be(booking.PassengerId.Value);
        result.DriverId.Should().Be(booking.DriverId);
        result.SeatsBooked.Should().Be(2);
        result.TotalPrice.Should().Be(1000m);
        result.Currency.Should().Be("RSD");
        result.Status.Should().Be(BookingStatus.Pending);
        result.BookedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_BookingNotFound_ReturnsNull()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepo
            .Setup(r => r.GetById(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity?)null);

        var query = new GetBookingByIdQuery { BookingId = bookingId };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ConfirmedBooking_MapsTimestampsCorrectly()
    {
        var booking = CreateBooking();
        booking.Confirm();

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var query = new GetBookingByIdQuery { BookingId = booking.Id };

        var result = await _handler.Handle(query, CancellationToken.None);

        result!.Status.Should().Be(BookingStatus.Confirmed);
        result.ConfirmedAt.Should().NotBeNull();
        result.CancelledAt.Should().BeNull();
        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CancelledBooking_MapsCancellationReason()
    {
        var booking = CreateBooking();
        booking.Cancel("No longer needed");

        _bookingRepo
            .Setup(r => r.GetById(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var query = new GetBookingByIdQuery { BookingId = booking.Id };

        var result = await _handler.Handle(query, CancellationToken.None);

        result!.Status.Should().Be(BookingStatus.Cancelled);
        result.CancellationReason.Should().Be("No longer needed");
        result.CancelledAt.Should().NotBeNull();
    }
}
