using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RideBuddy.E2E.Tests.Models;

namespace RideBuddy.E2E.Tests.Infrastructure;

public class ApiClient : IDisposable
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    // ── Auth ──────────────────────────────────────────────────

    public Task<HttpResponseMessage> RegisterRaw(RegisterRequest request) =>
        PostJson($"{ServiceUrls.UserService}/api/auth/register", request);

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        var response = await RegisterRaw(request);
        response.EnsureSuccessStatusCode();
        return await Deserialize<AuthResponse>(response);
    }

    public Task<HttpResponseMessage> LoginRaw(LoginRequest request) =>
        PostJson($"{ServiceUrls.UserService}/api/auth/login", request);

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var response = await LoginRaw(request);
        response.EnsureSuccessStatusCode();
        return await Deserialize<AuthResponse>(response);
    }

    // ── Rides ─────────────────────────────────────────────────

    public Task<HttpResponseMessage> CreateRideRaw(CreateRideRequest request, string token) =>
        PostJson($"{ServiceUrls.RideService}/api/rides", request, token);

    public async Task<RideResponse> CreateRide(CreateRideRequest request, string token)
    {
        var response = await CreateRideRaw(request, token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<RideResponse>(response);
    }

    public async Task<HttpResponseMessage> GetRideRaw(Guid id) =>
        await _http.GetAsync($"{ServiceUrls.RideService}/api/rides/{id}");

    public async Task<RideResponse> GetRide(Guid id)
    {
        var response = await GetRideRaw(id);
        response.EnsureSuccessStatusCode();
        return await Deserialize<RideResponse>(response);
    }

    public async Task<HttpResponseMessage> SearchRidesRaw(string? origin = null, string? destination = null) =>
        await _http.GetAsync(BuildSearchUrl(origin, destination));

    public async Task<List<RideResponse>> SearchRides(string? origin = null, string? destination = null)
    {
        var response = await SearchRidesRaw(origin, destination);
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<RideResponse>>(response);
    }

    public async Task<List<RideResponse>> GetMyRides(string token)
    {
        var response = await GetMyRidesRaw(token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<RideResponse>>(response);
    }

    public Task<HttpResponseMessage> GetMyRidesRaw(string token) =>
        SendWithAuth(HttpMethod.Get, $"{ServiceUrls.RideService}/api/rides/my-rides", token);

    public Task<HttpResponseMessage> StartRide(Guid id, string token) =>
        SendWithAuth(HttpMethod.Put, $"{ServiceUrls.RideService}/api/rides/{id}/start", token);

    public Task<HttpResponseMessage> CompleteRide(Guid id, string token) =>
        SendWithAuth(HttpMethod.Put, $"{ServiceUrls.RideService}/api/rides/{id}/complete", token);

    public Task<HttpResponseMessage> CancelRide(Guid id, string token, string? reason = null) =>
        PutJson($"{ServiceUrls.RideService}/api/rides/{id}/cancel", new { reason }, token);

    // ── Bookings ──────────────────────────────────────────────

    public Task<HttpResponseMessage> CreateBookingRaw(CreateBookingRequest request, string? token = null) =>
        PostJson($"{ServiceUrls.BookingService}/api/bookings", request, token);

    public async Task<BookingResponse> CreateBooking(CreateBookingRequest request, string token)
    {
        var response = await CreateBookingRaw(request, token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<BookingResponse>(response);
    }

    public async Task<BookingResponse> GetBooking(Guid id, string token)
    {
        var response = await SendWithAuth(HttpMethod.Get, $"{ServiceUrls.BookingService}/api/bookings/{id}", token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<BookingResponse>(response);
    }

    public async Task<List<BookingResponse>> GetMyBookings(string token)
    {
        var response = await GetMyBookingsRaw(token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<BookingResponse>>(response);
    }

    public Task<HttpResponseMessage> GetMyBookingsRaw(string token) =>
        SendWithAuth(HttpMethod.Get, $"{ServiceUrls.BookingService}/api/bookings/my-bookings", token);

    public Task<HttpResponseMessage> ConfirmBooking(Guid id, string token) =>
        SendWithAuth(HttpMethod.Put, $"{ServiceUrls.BookingService}/api/bookings/{id}/confirm", token);

    public Task<HttpResponseMessage> RejectBooking(Guid id, string token, string? reason = null) =>
        PutJson($"{ServiceUrls.BookingService}/api/bookings/{id}/reject", new { reason }, token);

    public Task<HttpResponseMessage> CancelBooking(Guid id, string token, string? reason = null) =>
        PutJson($"{ServiceUrls.BookingService}/api/bookings/{id}/cancel", new { reason }, token);

    // ── Notifications ─────────────────────────────────────────

    public async Task<List<NotificationResponse>> GetNotifications(string token, bool unreadOnly = false)
    {
        var url = $"{ServiceUrls.NotificationService}/api/notifications?unreadOnly={unreadOnly}";
        var response = await SendWithAuth(HttpMethod.Get, url, token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<List<NotificationResponse>>(response);
    }

    public async Task<UnreadCountResponse> GetUnreadCount(string token)
    {
        var response = await SendWithAuth(HttpMethod.Get,
            $"{ServiceUrls.NotificationService}/api/notifications/unread-count", token);
        response.EnsureSuccessStatusCode();
        return await Deserialize<UnreadCountResponse>(response);
    }

    public Task<HttpResponseMessage> MarkNotificationRead(Guid id, string token) =>
        SendWithAuth(HttpMethod.Put, $"{ServiceUrls.NotificationService}/api/notifications/{id}/read", token);

    public Task<HttpResponseMessage> MarkAllNotificationsRead(string token) =>
        SendWithAuth(HttpMethod.Put, $"{ServiceUrls.NotificationService}/api/notifications/read-all", token);

    public Task<HttpResponseMessage> GetNotificationsRaw(string token) =>
        SendWithAuth(HttpMethod.Get, $"{ServiceUrls.NotificationService}/api/notifications", token);

    // ── Helpers ───────────────────────────────────────────────

    private async Task<HttpResponseMessage> PostJson<T>(string url, T body, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _http.SendAsync(request);
    }

    private async Task<HttpResponseMessage> PutJson<T>(string url, T body, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _http.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendWithAuth(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    private static async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize response to {typeof(T).Name}: {json}");
    }

    private static string BuildSearchUrl(string? origin, string? destination)
    {
        var sb = new StringBuilder($"{ServiceUrls.RideService}/api/rides/search?");
        if (origin is not null) sb.Append($"origin={Uri.EscapeDataString(origin)}&");
        if (destination is not null) sb.Append($"destination={Uri.EscapeDataString(destination)}&");
        return sb.ToString().TrimEnd('&', '?');
    }

    public void Dispose() => _http.Dispose();
}
