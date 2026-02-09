using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Ride.Infrastructure.Persistence;

namespace Ride.Infrastructure.Outbox;

/// <summary>
/// Background service that processes undelivered messages from the outbox.
/// Ensures reliable message delivery even if RabbitMQ was temporarily unavailable.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _processInterval = TimeSpan.FromSeconds(10);
    private const string ExchangeName = "ridebuddy.events";
    private const int MaxRetries = 5;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ride Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processInterval, stoppingToken);
        }

        _logger.LogInformation("Ride Outbox processor stopped");
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RideDbContext>();
        var connection = scope.ServiceProvider.GetService<IConnection>();

        if (connection is null || !connection.IsOpen)
        {
            _logger.LogDebug("RabbitMQ connection not available, skipping outbox processing");
            return;
        }

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true);

        foreach (var message in messages)
        {
            try
            {
                var routingKey = $"ride.{message.EventType}";
                var body = Encoding.UTF8.GetBytes(message.Payload);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.Id.ToString();
                properties.Type = message.EventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;

                _logger.LogDebug("Published outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish outbox message {MessageId}", message.Id);
                message.RetryCount++;
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
