using System.Text;
using System.Text.Json;
using Booking.Application.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Booking.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes ride domain events from RabbitMQ.
/// When a ride is completed, automatically completes all confirmed bookings for that ride.
/// </summary>
public class RideEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RideEventConsumer> _logger;
    private readonly IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "ridebuddy.events";
    private const string QueueName = "booking-service.ride-events";
    private const string RoutingKeyPattern = "ride.#";

    public RideEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConnection? connection,
        ILogger<RideEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection is null || !_connection.IsOpen)
        {
            _logger.LogWarning("RabbitMQ connection not available. Ride event consumer will not start.");
            return Task.CompletedTask;
        }

        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true);

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKeyPattern);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                await HandleMessage(ea, stoppingToken);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ride event {MessageId}", ea.BasicProperties.MessageId);
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Ride event consumer started. Listening on queue '{Queue}' with pattern '{Pattern}'",
            QueueName, RoutingKeyPattern);

        return Task.CompletedTask;
    }

    private async Task HandleMessage(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var eventType = ea.BasicProperties.Type ?? ea.RoutingKey;

        _logger.LogInformation(
            "Received ride event: {EventType}, RoutingKey: {RoutingKey}",
            eventType, ea.RoutingKey);

        var normalizedType = eventType.ToLowerInvariant().Replace("ride.", "");

        switch (normalizedType)
        {
            case "ridecompletedevent":
                await HandleRideCompleted(body, ct);
                break;

            default:
                _logger.LogDebug("Ignoring ride event type: {EventType}", eventType);
                break;
        }
    }

    private async Task HandleRideCompleted(string payload, CancellationToken ct)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evt = JsonSerializer.Deserialize<RideCompletedEventDto>(payload, options);

        if (evt is null)
        {
            _logger.LogWarning("Could not deserialize RideCompletedEvent: {Payload}", payload);
            return;
        }

        _logger.LogInformation(
            "Processing RideCompletedEvent for ride {RideId} by driver {DriverId}",
            evt.RideId, evt.DriverId);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var bookings = await unitOfWork.Bookings.GetByRideId(evt.RideId, ct);
        var confirmedBookings = bookings.Where(b => b.Status == BookingStatus.Confirmed).ToList();

        if (confirmedBookings.Count == 0)
        {
            _logger.LogInformation("No confirmed bookings to complete for ride {RideId}", evt.RideId);
            return;
        }

        var completedCount = 0;

        foreach (var booking in confirmedBookings)
        {
            try
            {
                booking.Complete();
                await unitOfWork.Bookings.Update(booking, ct);
                completedCount++;

                _logger.LogInformation(
                    "Booking {BookingId} auto-completed for ride {RideId}",
                    booking.Id, evt.RideId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to complete booking {BookingId} for ride {RideId}",
                    booking.Id, evt.RideId);
            }
        }

        if (completedCount == 0)
        {
            _logger.LogWarning("No bookings were successfully completed for ride {RideId}", evt.RideId);
            return;
        }

        // Collect domain events before saving
        var allEvents = confirmedBookings
            .Where(b => b.Status == BookingStatus.Completed)
            .SelectMany(b =>
            {
                var events = b.DomainEvents.ToList();
                b.ClearDomainEvents();
                return events;
            })
            .ToList();

        // PublishMany adds outbox messages to the same DbContext and calls SaveChanges,
        // which atomically persists both booking status changes AND outbox messages
        // in a single transaction (Outbox pattern guarantee).
        await eventPublisher.PublishMany(allEvents, ct);

        _logger.LogInformation(
            "Auto-completed {Count} bookings for ride {RideId}",
            completedCount, evt.RideId);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// DTO for deserializing RideCompletedEvent from RabbitMQ.
/// </summary>
internal record RideCompletedEventDto
{
    public Guid RideId { get; init; }
    public Guid DriverId { get; init; }
    public DateTime CompletedAt { get; init; }
}
