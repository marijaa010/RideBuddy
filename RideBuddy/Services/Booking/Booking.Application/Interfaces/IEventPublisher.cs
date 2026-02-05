using Booking.Domain.Common;

namespace Booking.Application.Interfaces;

/// <summary>
/// Interface for publishing domain events to a message broker.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to RabbitMQ.
    /// </summary>
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) 
        where T : DomainEvent;

    /// <summary>
    /// Publishes multiple events at once.
    /// </summary>
    Task PublishManyAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
