using System.Text;
using System.Text.Json;
using Booking.Application.Interfaces;
using Booking.Domain.Common;
using Booking.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Booking.Infrastructure.Messaging;

/// <summary>
/// Event publisher using RabbitMQ with Outbox pattern for reliable delivery.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly BookingDbContext _dbContext;
    private readonly IConnection? _connection;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private const string ExchangeName = "ridebuddy.events";

    public RabbitMqEventPublisher(
        BookingDbContext dbContext,
        IConnection? connection,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _dbContext = dbContext;
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) 
        where T : DomainEvent
    {
        // Save to outbox first (transactional guarantee)
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.EventType,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Try to publish immediately
        await TryPublishToRabbitMq(outboxMessage);
    }

    public async Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var outboxMessages = domainEvents.Select(e => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = e.EventType,
            Payload = JsonSerializer.Serialize(e, e.GetType()),
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _dbContext.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Try to publish immediately
        foreach (var message in outboxMessages)
        {
            await TryPublishToRabbitMq(message);
        }
    }

    private async Task TryPublishToRabbitMq(OutboxMessage message)
    {
        if (_connection is null || !_connection.IsOpen)
        {
            _logger.LogWarning("RabbitMQ connection not available. Message {MessageId} saved to outbox.", message.Id);
            return;
        }

        try
        {
            using var channel = _connection.CreateModel();
            
            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true);

            var routingKey = $"booking.{message.EventType.ToLowerInvariant()}";
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

            // Mark as processed
            message.ProcessedAt = DateTime.UtcNow;
            _dbContext.OutboxMessages.Update(message);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Published event {EventType} with routing key {RoutingKey}", 
                message.EventType, 
                routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageId} to RabbitMQ", message.Id);
            message.Error = ex.Message;
            message.RetryCount++;
            _dbContext.OutboxMessages.Update(message);
            await _dbContext.SaveChangesAsync();
        }
    }
}
