using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Features.Auctions.Common;

public sealed record AuctionDto(
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
    DateTime CreatedAt)
{
    public static AuctionDto FromEntity(Auction a) => new(
        a.Id, a.Title, a.Description, a.ImageUrl,
        a.SellerId, a.Seller.DisplayName,
        a.CategoryId, a.Category.Name,
        a.StartPrice, a.CurrentPrice, a.ReservePrice, a.BuyItNowPrice, a.MinBidIncrement,
        a.Status.ToString(), a.StartsAt, a.EndsAt, a.ExtensionCount,
        a.IsReserveMet, a.IsBuyItNowAvailable, a.CreatedAt);
}