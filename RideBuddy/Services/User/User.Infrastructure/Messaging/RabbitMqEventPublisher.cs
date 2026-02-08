using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SharedKernel;
using User.Application.Interfaces;
using User.Infrastructure.Persistence;

namespace User.Infrastructure.Messaging;

/// <summary>
/// RabbitMQ event publisher using the Outbox pattern.
/// Events are saved to the database first, then published by the OutboxProcessor.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly UserDbContext _context;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(UserDbContext context, ILogger<RabbitMqEventPublisher> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Publish(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.EventType,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            CreatedAt = DateTime.UtcNow
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved event {EventType} to outbox", domainEvent.EventType);
    }

    public async Task PublishMany(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await Publish(domainEvent, cancellationToken);
        }
    }
}
