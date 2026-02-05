namespace Booking.Domain.Common;

/// <summary>
/// Base class for aggregate root entities.
/// An aggregate root is the main entity that encapsulates a group of related objects
/// and controls access to them.
/// </summary>
public abstract class AggregateRoot : Entity
{
    /// <summary>
    /// Version of the aggregate for optimistic concurrency control.
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Increments the aggregate version.
    /// </summary>
    protected void IncrementVersion()
    {
        Version++;
    }
}
