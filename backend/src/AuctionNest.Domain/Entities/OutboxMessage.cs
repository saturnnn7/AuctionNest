using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Entities;

/// <summary>
/// Outbox pattern: domain events are written here atomically with the
/// business transaction. A background worker processes and dispatches them.
/// </summary>
public sealed class OutboxMessage : Entity
{
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    public static OutboxMessage Create(string eventType, string payload)
        => new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            RetryCount = 0
        };

    public void MarkProcessed()
        => ProcessedAt = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}