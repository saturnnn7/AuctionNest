using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Events;

public sealed record AuctionStartedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid AuctionId) : IDomainEvent
{
    public static AuctionStartedEvent Create(Guid auctionId)
        => new(Guid.NewGuid(), DateTime.UtcNow, auctionId);
}