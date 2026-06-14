using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Events;

public sealed record BidPlacedEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid AuctionId,
    Guid BidderId,
    decimal Amount,
    decimal NewCurrentPrice,
    Guid? PreviousWinnerBidderId) : IDomainEvent
{
    public static BidPlacedEvent Create(
        Guid auctionId, Guid bidderId,
        decimal amount, decimal newCurrentPrice,
        Guid? previousWinnerBidderId)
        => new(Guid.NewGuid(), DateTime.UtcNow,
            auctionId, bidderId, amount, newCurrentPrice, previousWinnerBidderId);
}