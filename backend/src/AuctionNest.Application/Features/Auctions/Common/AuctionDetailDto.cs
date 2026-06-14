using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Features.Auctions.Common;

public sealed record AuctionDetailDto(
    Guid Id,
    string Title,
    string Description,
    string? ImageUrl,
    Guid SellerId,
    string SellerDisplayName,
    Guid CategoryId,
    string CategoryName,
    decimal StartPrice,
    decimal CurrentPrice,
    decimal? ReservePrice,
    decimal? BuyItNowPrice,
    decimal MinBidIncrement,
    string Status,
    DateTime StartsAt,
    DateTime EndsAt,
    int ExtensionCount,
    bool IsReserveMet,
    bool IsBuyItNowAvailable,
    DateTime CreatedAt,
    IReadOnlyList<BidDto> RecentBids,
    int TotalBids)
{
    public static AuctionDetailDto FromEntity(Auction a)
    {
        var bids = a.Bids
            .OrderByDescending(b => b.Amount)
            .Take(20)
            .Select(b => BidDto.FromEntity(b, a.CurrentPrice))
            .ToList();

        return new(
            a.Id, a.Title, a.Description, a.ImageUrl,
            a.SellerId, a.Seller.DisplayName,
            a.CategoryId, a.Category.Name,
            a.StartPrice, a.CurrentPrice, a.ReservePrice, a.BuyItNowPrice, a.MinBidIncrement,
            a.Status.ToString(), a.StartsAt, a.EndsAt, a.ExtensionCount,
            a.IsReserveMet, a.IsBuyItNowAvailable, a.CreatedAt,
            bids, a.Bids.Count);
    }
}