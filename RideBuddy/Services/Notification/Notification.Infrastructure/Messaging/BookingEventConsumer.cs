using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes booking domain events from RabbitMQ
/// and dispatches them to the NotificationService.
/// 
/// Listens on the "ridebuddy.events" topic exchange with routing key "booking.#"
/// which matches the Booking Service publisher format.
/// </summary>
public class BookingEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingEventConsumer> _logger;
    private readonly IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "ridebuddy.events";
    private const string QueueName = "notification-service.booking-events";
    private const string RoutingKeyPattern = "booking.#";

    public BookingEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConnection? connection,
        ILogger<BookingEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _connection = connection;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_connection is null || !_connection.IsOpen)
        {
            _logger.LogWarning("RabbitMQ connection not available. Consumer will not start.");
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

        // Process one message at a time for simplicity
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
                _logger.LogError(ex, "Error processing message {MessageId}", ea.BasicProperties.MessageId);
                // Requeue on failure so we don't lose messages
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Booking event consumer started. Listening on queue '{Queue}' with pattern '{Pattern}'",
            QueueName, RoutingKeyPattern);

        return Task.CompletedTask;
    }

    private async Task HandleMessage(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var eventType = ea.BasicProperties.Type ?? ea.RoutingKey;

        _logger.LogInformation(
            "Received event: {EventType}, RoutingKey: {RoutingKey}",
            eventType, ea.RoutingKey);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evt = JsonSerializer.Deserialize<BookingEventDto>(body, options);

        if (evt is null)
        {
            _logger.LogWarning("Could not deserialize event body: {Body}", body);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var normalizedType = eventType.ToLowerInvariant().Replace("booking.", "");

        switch (normalizedType)
        {
            case "bookingcreatedevent":
                await notificationService.HandleBookingCreated(evt, ct);
                break;

            case "bookingconfirmedevent":
                await notificationService.HandleBookingConfirmed(evt, ct);
                break;

            case "bookingrejectedevent":
                await notificationService.HandleBookingRejected(evt, ct);
                break;

            case "bookingcancelledevent":
                await notificationService.HandleBookingCancelled(evt, ct);
                break;

            case "bookingcompletedevent":
                await notificationService.HandleBookingCompleted(evt, ct);
                break;

            default:
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
                break;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}
