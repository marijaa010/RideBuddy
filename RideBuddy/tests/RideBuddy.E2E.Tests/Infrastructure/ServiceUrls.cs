namespace RideBuddy.E2E.Tests.Infrastructure;

public static class ServiceUrls
{
    public static string UserService =>
        Environment.GetEnvironmentVariable("E2E_USER_SERVICE_URL")
        ?? "http://localhost:5001";

    public static string RideService =>
        Environment.GetEnvironmentVariable("E2E_RIDE_SERVICE_URL")
        ?? "http://localhost:5002";

    public static string BookingService =>
        Environment.GetEnvironmentVariable("E2E_BOOKING_SERVICE_URL")
        ?? "http://localhost:5003";

    public static string NotificationService =>
        Environment.GetEnvironmentVariable("E2E_NOTIFICATION_SERVICE_URL")
        ?? "http://localhost:5004";
}
