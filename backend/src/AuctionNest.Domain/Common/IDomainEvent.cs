namespace AuctionNest.Domain.Common;

/// <summary>
/// Represents a domain event.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}