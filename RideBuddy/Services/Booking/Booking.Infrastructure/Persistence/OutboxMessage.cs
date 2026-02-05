namespace Booking.Infrastructure.Persistence;

/// <summary>
/// Entity representing a message in the outbox table.
/// Used to ensure reliable message publishing (Outbox Pattern).
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The type of the event (e.g., "BookingConfirmedEvent").
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized event payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the message was processed (published to message broker).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Error message if publishing failed.
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int RetryCount { get; set; }
}
