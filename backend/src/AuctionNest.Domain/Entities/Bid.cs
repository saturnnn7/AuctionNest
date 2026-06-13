using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Entities;

public sealed class Bid : Entity
{
    public Guid AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public decimal Amount { get; private set; }
    public bool IsWinning { get; private set; }

    public Auction Auction { get; private set; } = null!;
    public User Bidder { get; private set; } = null!;

    internal static Bid Create(Guid auctionId, Guid bidderId, decimal amount)
        => new()
        {
            Id = Guid.NewGuid(),
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            IsWinning = true
        };

    internal void SetNotWinning()
        => IsWinning = false;
}