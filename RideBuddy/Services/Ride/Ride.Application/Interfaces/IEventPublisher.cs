using SharedKernel;

namespace Ride.Application.Interfaces;

/// <summary>
/// Interface for publishing domain events to a message broker.
/// </summary>
public interface IEventPublisher
{
    Task Publish<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent;
    Task PublishMany(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
