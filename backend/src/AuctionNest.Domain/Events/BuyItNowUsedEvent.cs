using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Events;

public sealed record BuyItNowUsedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid AuctionId,
    Guid BuyerId,
    decimal Amount) : IDomainEvent
{
    public static BuyItNowUsedEvent Create(Guid auctionId,
        Guid buyerId, decimal amount)
        => new(Guid.NewGuid(), DateTime.UtcNow, auctionId, buyerId, amount);
}