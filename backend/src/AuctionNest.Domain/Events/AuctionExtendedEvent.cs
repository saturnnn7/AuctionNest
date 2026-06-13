using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Events;

public sealed record AuctionExtendedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid AuctionId,
    DateTime NewEndsAt,
    int ExtensionCount) : IDomainEvent
{
    public static AuctionExtendedEvent Create(Guid auctionId,
        DateTime newEndsAt, int extensionCount)
        => new(Guid.NewGuid(), DateTime.UtcNow,
            auctionId, newEndsAt, extensionCount);
}