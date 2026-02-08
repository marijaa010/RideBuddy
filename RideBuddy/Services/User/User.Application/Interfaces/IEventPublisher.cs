using SharedKernel;

namespace User.Application.Interfaces;

/// <summary>
/// Interface for publishing domain events to a message broker.
/// </summary>
public interface IEventPublisher
{
    Task Publish(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishMany(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
