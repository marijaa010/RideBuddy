using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Domain.Enums;

namespace Notification.Application.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _repository;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<IUserGrpcClient> _userClient;
    private readonly Mock<IRealTimeNotifier> _realTimeNotifier;
    private readonly NotificationService _service;

    private readonly Guid _passengerId;
    private readonly Guid _driverId;
    private readonly Guid _bookingId;
    private readonly Guid _rideId;
    private readonly UserInfoDto _passenger;
    private readonly UserInfoDto _driver;

    public NotificationServiceTests()
    {
        _passengerId = Guid.NewGuid();
        _driverId    = Guid.NewGuid();
        _bookingId   = Guid.NewGuid();
        _rideId      = Guid.NewGuid();

        _passenger = new UserInfoDto
        {
            UserId    = _passengerId,
            Email     = "passenger@test.com",
            FirstName = "Ana",
            LastName  = "Anic"
        };

        _driver = new UserInfoDto
        {
            UserId    = _driverId,
            Email     = "driver@test.com",
            FirstName = "Marko",
            LastName  = "Markovic"
        };

        _repository       = new Mock<INotificationRepository>();
        _emailService     = new Mock<IEmailService>();
        _userClient       = new Mock<IUserGrpcClient>();
        _realTimeNotifier = new Mock<IRealTimeNotifier>();

        _service = new NotificationService(
            _repository.Object,
            _emailService.Object,
            _userClient.Object,
            _realTimeNotifier.Object,
            Mock.Of<ILogger<NotificationService>>());
    }

    private BookingEventDto CreateEvent() => new()
    {
        BookingId          = _bookingId,
        RideId             = _rideId,
        PassengerId        = _passengerId,
        DriverId           = _driverId,
        SeatsBooked        = 2,
        SeatsReleased      = 2,
        TotalPrice         = 1000m,
        Currency           = "RSD",
        IsAutoConfirmed    = false,
        CancelledByPassenger = false,
        DepartureTime      = DateTime.UtcNow.AddHours(24)
    };

    private void SetupUsers()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_passenger);
        _userClient.Setup(u => u.GetUserInfo(_driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_driver);
    }

    // ── HandleBookingCreated ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleBookingCreated_HappyPath_SavesNotificationForBothUsers()
    {
        SetupUsers();

        await _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleBookingCreated_HappyPath_SendsSignalRToBothUsers()
    {
        SetupUsers();

        await _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        _realTimeNotifier.Verify(
            r => r.SendToUser(_passengerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_driverId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCreated_HappyPath_SendsEmailToBothUsers()
    {
        SetupUsers();

        await _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(_passenger.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_driver.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCreated_AutoConfirmed_PassengerEmailContainsAutoConfirmedText()
    {
        SetupUsers();
        var evt = CreateEvent() with { IsAutoConfirmed = true };

        await _service.HandleBookingCreated(evt, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                _passenger.Email, It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(body => body.Contains("auto-confirmed")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCreated_ManualApproval_PassengerEmailContainsWaitingForDriverText()
    {
        SetupUsers();
        var evt = CreateEvent() with { IsAutoConfirmed = false };

        await _service.HandleBookingCreated(evt, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                _passenger.Email, It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(body => body.Contains("Waiting for driver")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCreated_PassengerNotFound_DoesNotSendAnyNotification()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        await _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailService.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleBookingCreated_DriverNotFound_OnlyNotifiesPassenger()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_passenger);
        _userClient.Setup(u => u.GetUserInfo(_driverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        await _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_driver.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleBookingCreated_SignalRFails_StillSendsEmails()
    {
        SetupUsers();
        _realTimeNotifier
            .Setup(r => r.SendToUser(It.IsAny<Guid>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SignalR unavailable"));

        await _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleBookingCreated_EmailFails_DoesNotThrow()
    {
        SetupUsers();
        _emailService
            .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP unavailable"));

        var act = () => _service.HandleBookingCreated(CreateEvent(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── HandleBookingConfirmed ────────────────────────────────────────────────

    [Fact]
    public async Task HandleBookingConfirmed_HappyPath_SendsAllChannelsToPassenger()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_passenger);

        await _service.HandleBookingConfirmed(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_passengerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_passenger.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingConfirmed_PassengerNotFound_DoesNotSendNotification()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        await _service.HandleBookingConfirmed(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── HandleBookingRejected ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleBookingRejected_HappyPath_NotifiesPassenger()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_passenger);

        await _service.HandleBookingRejected(
            CreateEvent() with { RejectionReason = "No room" }, CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_passenger.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingRejected_NoReason_UsesDefaultReason()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_passenger);

        await _service.HandleBookingRejected(
            CreateEvent() with { RejectionReason = "" }, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                _passenger.Email, It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(body => body.Contains("No reason provided")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingRejected_PassengerNotFound_DoesNotSendNotification()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        await _service.HandleBookingRejected(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── HandleBookingCancelled ────────────────────────────────────────────────

    [Fact]
    public async Task HandleBookingCancelled_CancelledByPassenger_PassengerGetsEmailOnly()
    {
        SetupUsers();
        var evt = CreateEvent() with { CancelledByPassenger = true };

        await _service.HandleBookingCancelled(evt, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(_passenger.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_passengerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleBookingCancelled_CancelledByPassenger_DriverGetsSignalRAndEmail()
    {
        SetupUsers();
        var evt = CreateEvent() with { CancelledByPassenger = true };

        await _service.HandleBookingCancelled(evt, CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_driverId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_driver.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCancelled_CancelledByDriver_DriverGetsEmailOnly()
    {
        SetupUsers();
        var evt = CreateEvent() with { CancelledByPassenger = false };

        await _service.HandleBookingCancelled(evt, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(_driver.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_driverId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleBookingCancelled_CancelledByDriver_PassengerGetsSignalRAndEmail()
    {
        SetupUsers();
        var evt = CreateEvent() with { CancelledByPassenger = false };

        await _service.HandleBookingCancelled(evt, CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_passengerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_passenger.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCancelled_LateCancellation_EmailContainsWarning()
    {
        SetupUsers();
        var evt = CreateEvent() with
        {
            CancelledByPassenger = true,
            DepartureTime        = DateTime.UtcNow.AddMinutes(30),
            CancelledAt          = DateTime.UtcNow
        };

        await _service.HandleBookingCancelled(evt, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                _passenger.Email, It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(body => body.Contains("less than 1 hour before departure")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCancelled_NotLateCancellation_EmailDoesNotContainWarning()
    {
        SetupUsers();
        var evt = CreateEvent() with
        {
            CancelledByPassenger = true,
            DepartureTime        = DateTime.UtcNow.AddHours(3),
            CancelledAt          = DateTime.UtcNow
        };

        await _service.HandleBookingCancelled(evt, CancellationToken.None);

        _emailService.Verify(
            e => e.SendAsync(
                _passenger.Email, It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(body => !body.Contains("less than 1 hour before departure")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── HandleBookingCompleted ────────────────────────────────────────────────

    [Fact]
    public async Task HandleBookingCompleted_HappyPath_SendsAllChannelsToPassenger()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_passenger);

        await _service.HandleBookingCompleted(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _realTimeNotifier.Verify(
            r => r.SendToUser(_passengerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(
            e => e.SendAsync(_passenger.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleBookingCompleted_PassengerNotFound_DoesNotSendNotification()
    {
        _userClient.Setup(u => u.GetUserInfo(_passengerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfoDto?)null);

        await _service.HandleBookingCompleted(CreateEvent(), CancellationToken.None);

        _repository.Verify(
            r => r.Add(It.IsAny<global::Notification.Domain.Entities.NotificationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
