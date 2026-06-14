using AuctionNest.Domain.Entities;

namespace AuctionNest.UnitTests.Domain.Helpers;

// Creates test auction instances with sensible defaults
public static class AuctionFactory
{
    public static readonly Guid DefaultSellerId   = Guid.NewGuid();
    public static readonly Guid DefaultCategoryId = Guid.NewGuid();
    public static readonly Guid DefaultBidderId   = Guid.NewGuid();

    public static Auction CreateDraft(
        decimal startPrice       = 100m,
        decimal minBidIncrement  = 10m,
        decimal? reservePrice    = null,
        decimal? buyItNowPrice   = null,
        DateTime? endsAt         = null,
        Guid? sellerId           = null)
    {
        return Auction.Create(
            sellerId      ?? DefaultSellerId,
            DefaultCategoryId,
            "Test Auction",
            "Test Description",
            startPrice,
            minBidIncrement,
            DateTime.UtcNow.AddHours(1),
            endsAt ?? DateTime.UtcNow.AddDays(1),
            reservePrice,
            buyItNowPrice);
    }

    public static Auction CreateActive(
        decimal startPrice       = 100m,
        decimal minBidIncrement  = 10m,
        decimal? reservePrice    = null,
        decimal? buyItNowPrice   = null,
        DateTime? endsAt         = null,
        Guid? sellerId           = null)
    {
        var auction = CreateDraft(startPrice, minBidIncrement,
            reservePrice, buyItNowPrice, endsAt, sellerId);
        auction.Activate();
        return auction;
    }

    // Active auction ending in N seconds — for anti-snipe tests
    public static Auction CreateActiveEndingSoon(int secondsUntilEnd = 15)
        => CreateActive(endsAt: DateTime.UtcNow.AddSeconds(secondsUntilEnd));
}