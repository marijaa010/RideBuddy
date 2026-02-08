namespace SharedKernel;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; }

    /// <summary>
    /// Type of the event, used for serialization.
    /// </summary>
    public string EventType => GetType().Name;

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}
