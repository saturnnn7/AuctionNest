using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Features.Auctions.Common;

public sealed record BidDto(
    Guid Id,
    Guid AuctionId,
    Guid BidderId,
    decimal Amount,
    bool IsWinning,
    decimal NewCurrentPrice,
    DateTime PlacedAt)
{
    public static BidDto FromEntity(Bid bid, decimal newCurrentPrice)
        => new(bid.Id, bid.AuctionId, bid.BidderId,
               bid.Amount, bid.IsWinning, newCurrentPrice, bid.CreatedAt);
}