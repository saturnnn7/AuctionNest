using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Events;

public sealed record AuctionCancelledEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid AuctionId) : IDomainEvent
{
    public static AuctionCancelledEvent Create(Guid auctionId)
        => new(Guid.NewGuid(), DateTime.UtcNow, auctionId);
}