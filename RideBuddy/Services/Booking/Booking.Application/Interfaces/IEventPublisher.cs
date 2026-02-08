using SharedKernel;

namespace Booking.Application.Interfaces;

/// <summary>
/// Interface for publishing domain events to a message broker.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to RabbitMQ.
    /// </summary>
    Task Publish<T>(T domainEvent, CancellationToken cancellationToken = default) 
        where T : DomainEvent;

    /// <summary>
    /// Publishes multiple events at once.
    /// </summary>
    Task PublishMany(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
