using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Application.Services;

/// <summary>
/// Orchestrates notification delivery across all channels:
/// email, in-app (database), and real-time (SignalR).
/// </summary>
public class NotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IEmailService _emailService;
    private readonly IUserGrpcClient _userClient;
    private readonly IRealTimeNotifier _realTimeNotifier;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repository,
        IEmailService emailService,
        IUserGrpcClient userClient,
        IRealTimeNotifier realTimeNotifier,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _userClient = userClient;
        _realTimeNotifier = realTimeNotifier;
        _logger = logger;
    }

    public async Task HandleBookingCreated(BookingEventDto evt, CancellationToken ct)
    {
        var passenger = await _userClient.GetUserInfo(evt.PassengerId, ct);
        var isAutoConfirmed = evt.IsAutoConfirmed;
        var seatsBooked = $"{evt.SeatsBooked} seat" + (evt.SeatsBooked > 1 ? "s" : "");
        if (passenger is not null)
        {
            var title = "Booking submitted";
            var message = $"Your booking (#{evt.BookingId.ToString()[..8]}) for {seatsBooked} " +
                          $"has been submitted. Total: {evt.TotalPrice} {evt.Currency}. " +
                          (isAutoConfirmed ? "The booking was auto-confirmed by the system. Have a safe trip!" :
                          "Waiting for driver confirmation.");

            await SendAll(
                passenger, title, message,
                NotificationType.BookingCreated,
                evt.BookingId, evt.RideId,
                "RideBuddy - Booking submitted",
                BuildEmailBody(passenger.FirstName, title, message),
                ct);
        }

        var driver = await _userClient.GetUserInfo(evt.DriverId, ct);
        if (driver is not null && passenger is not null)
        {
            var driverTitle = "New booking request";
            var driverMessage = $"Passenger {passenger.FullName} has requested to book " +
                                $"{seatsBooked} on your ride (#{evt.RideId.ToString()[..8]}). " +
                                (isAutoConfirmed ? "The booking was auto-confirmed by the system." :
                                "Please confirm or reject the booking.");

            await SendAll(
                driver, driverTitle, driverMessage,
                NotificationType.BookingCreated,
                evt.BookingId, evt.RideId,
                "RideBuddy - New booking request",
                BuildEmailBody(driver.FirstName, driverTitle, driverMessage),
                ct);
        }
    }

    public async Task HandleBookingConfirmed(BookingEventDto evt, CancellationToken ct)
    {
        var passenger = await _userClient.GetUserInfo(evt.PassengerId, ct);
        if (passenger is null) return;
        var seatsBooked = $"{evt.SeatsBooked} seat" + (evt.SeatsReleased > 1 ? "s" : "");

        var title = "Booking confirmed!";
        var message = $"Great news! Your booking (#{evt.BookingId.ToString()[..8]}) " +
                      $"for {seatsBooked} has been confirmed by the driver. " +
                      $"Total: {evt.TotalPrice} RSD. Have a safe trip!";

        await SendAll(
            passenger, title, message,
            NotificationType.BookingConfirmed,
            evt.BookingId, evt.RideId,
            "RideBuddy - Booking confirmed",
            BuildEmailBody(passenger.FirstName, title, message),
            ct);
    }

    public async Task HandleBookingRejected(BookingEventDto evt, CancellationToken ct)
    {
        var passenger = await _userClient.GetUserInfo(evt.PassengerId, ct);
        if (passenger is null) return;

        var reason = string.IsNullOrWhiteSpace(evt.RejectionReason)
            ? "No reason provided"
            : evt.RejectionReason;

        var title = "Booking rejected";
        var message = $"Unfortunately, your booking (#{evt.BookingId.ToString()[..8]}) " +
                      $"was rejected by the driver. Reason: {reason}. " +
                      "The reserved seats have been released. Please try another ride.";

        await SendAll(
            passenger, title, message,
            NotificationType.BookingRejected,
            evt.BookingId, evt.RideId,
            "RideBuddy - Booking rejected",
            BuildEmailBody(passenger.FirstName, title, message),
            ct);
    }

    public async Task HandleBookingCancelled(BookingEventDto evt, CancellationToken ct)
    {
        var reason = string.IsNullOrWhiteSpace(evt.CancellationReason)
            ? "No reason provided"
            : evt.CancellationReason;
        var seatsReleased = $"{evt.SeatsReleased} seat" + (evt.SeatsReleased > 1 ? "s have" : " has");

        var isLateCancellation = evt.DepartureTime > DateTime.MinValue &&
                                 (evt.DepartureTime - evt.CancelledAt).TotalHours < 1;
        var lateWarning = isLateCancellation
            ? " Please note: this cancellation was made less than 1 hour before departure. " +
              "We kindly ask you to avoid late cancellations in the future."
            : string.Empty;

        if (evt.CancelledByPassenger)
        {
            // Passenger cancelled: passenger gets email only, driver gets email + push
            var passenger = await _userClient.GetUserInfo(evt.PassengerId, ct);
            if (passenger is not null)
            {
                var title = "Booking cancelled";
                var message = $"Your booking (#{evt.BookingId.ToString()[..8]}) has been cancelled. " +
                              $"Reason: {reason}. {seatsReleased} been released.{lateWarning}";

                await SendEmailOnly(
                    passenger,
                    "RideBuddy - Booking cancelled",
                    BuildEmailBody(passenger.FirstName, title, message),
                    ct);
            }

            var driver = await _userClient.GetUserInfo(evt.DriverId, ct);
            if (driver is not null)
            {
                var passengerName = passenger?.FullName ?? "A passenger";
                var driverTitle = "Booking cancelled by passenger";
                var driverMessage = $"{passengerName} has cancelled their booking " +
                                    $"(#{evt.BookingId.ToString()[..8]}) on your ride " +
                                    $"(#{evt.RideId.ToString()[..8]}). " +
                                    $"Reason: {reason}. {seatsReleased} been released back to the ride.";

                await SendAll(
                    driver, driverTitle, driverMessage,
                    NotificationType.BookingCancelled,
                    evt.BookingId, evt.RideId,
                    "RideBuddy - Booking cancelled by passenger",
                    BuildEmailBody(driver.FirstName, driverTitle, driverMessage),
                    ct);
            }
        }
        else
        {
            // Driver cancelled: driver gets email only, passenger gets email + push
            var driver = await _userClient.GetUserInfo(evt.DriverId, ct);
            if (driver is not null)
            {
                var driverTitle = "You cancelled a booking";
                var driverMessage = $"You have cancelled the booking (#{evt.BookingId.ToString()[..8]}). " +
                                    $"Reason: {reason}. {seatsReleased} have been released.{lateWarning}";

                await SendEmailOnly(
                    driver,
                    "RideBuddy - Booking cancelled",
                    BuildEmailBody(driver.FirstName, driverTitle, driverMessage),
                    ct);
            }

            var passenger = await _userClient.GetUserInfo(evt.PassengerId, ct);
            if (passenger is not null)
            {
                var passengerTitle = "Your booking was cancelled by the driver";
                var passengerMessage = $"Unfortunately, your booking (#{evt.BookingId.ToString()[..8]}) " +
                                       $"has been cancelled by the driver. " +
                                       $"Reason: {reason}. {seatsReleased} have been released. " +
                                       "Please look for another ride.";

                await SendAll(
                    passenger, passengerTitle, passengerMessage,
                    NotificationType.BookingCancelled,
                    evt.BookingId, evt.RideId,
                    "RideBuddy - Your booking was cancelled by the driver",
                    BuildEmailBody(passenger.FirstName, passengerTitle, passengerMessage),
                    ct);
            }
        }
    }

    public async Task HandleBookingCompleted(BookingEventDto evt, CancellationToken ct)
    {
        var passenger = await _userClient.GetUserInfo(evt.PassengerId, ct);
        if (passenger is null) return;

        var title = "Ride Completed!";
        var message = $"Your ride (booking #{evt.BookingId.ToString()[..8]}) has been completed. " +
                      "Thank you for riding with RideBuddy! We hope you had a great trip.";

        await SendAll(
            passenger, title, message,
            NotificationType.BookingCompleted,
            evt.BookingId, evt.RideId,
            "RideBuddy - Ride Completed",
            BuildEmailBody(passenger.FirstName, title, message),
            ct);
    }

    private async Task SendAll(
        UserInfoDto user,
        string title, string message,
        NotificationType type,
        Guid bookingId, Guid rideId,
        string emailSubject, string emailBody,
        CancellationToken ct)
    {
        var notification = NotificationEntity.Create(
            user.UserId, title, message, type, bookingId, rideId);

        await _repository.Add(notification, ct);

        var dto = MapToDto(notification);

        try
        {
            await _realTimeNotifier.SendToUser(user.UserId, dto, ct);
            _logger.LogInformation("SignalR push sent to user {UserId}", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR notification to user {UserId}", user.UserId);
        }

        try
        {
            await _emailService.SendAsync(
                user.Email, user.FullName,
                emailSubject, emailBody, ct);
            _logger.LogInformation("Email sent to {Email} for {Type}", user.Email, type);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {Email} for {Type}", user.Email, type);
        }
    }

    private async Task SendEmailOnly(
        UserInfoDto user,
        string emailSubject, string emailBody,
        CancellationToken ct)
    {
        try
        {
            await _emailService.SendAsync(
                user.Email, user.FullName,
                emailSubject, emailBody, ct);
            _logger.LogInformation("Email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {Email}", user.Email);
        }
    }

    private static string BuildEmailBody(string firstName, string title, string message)
    {
        return "<!DOCTYPE html>" +
            "<html><head><style>" +
            "body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }" +
            ".container { max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }" +
            ".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }" +
            ".header h1 { margin: 0; font-size: 24px; }" +
            ".content { padding: 30px; color: #333; line-height: 1.6; }" +
            ".content h2 { color: #667eea; margin-top: 0; }" +
            ".footer { padding: 20px 30px; background: #f9f9f9; color: #999; font-size: 12px; text-align: center; }" +
            "</style></head><body>" +
            "<div class='container'>" +
            "<div class='header'><h1>RideBuddy</h1></div>" +
            "<div class='content'>" +
            "<h2>" + title + "</h2>" +
            "<p>Hi " + firstName + ",</p>" +
            "<p>" + message + "</p>" +
            "<p>Best regards,<br>The RideBuddy Team</p>" +
            "</div>" +
            "<div class='footer'><p>This is an automated notification from RideBuddy.</p></div>" +
            "</div></body></html>";
    }

    private static NotificationDto MapToDto(NotificationEntity entity)
    {
        return new NotificationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            Message = entity.Message,
            Type = entity.Type,
            BookingId = entity.BookingId,
            RideId = entity.RideId,
            IsRead = entity.IsRead,
            CreatedAt = entity.CreatedAt
        };
    }
}
