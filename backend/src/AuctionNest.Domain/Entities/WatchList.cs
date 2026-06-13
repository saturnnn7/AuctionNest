using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Entities;

public sealed class WatchList : Entity
{
    public Guid UserId { get; private set; }
    public Guid AuctionId { get; private set; }

    public User User { get; private set; } = null!;
    public Auction Auction { get; private set; } = null!;

    public static WatchList Create(Guid userId, Guid auctionId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AuctionId = auctionId
        };
}