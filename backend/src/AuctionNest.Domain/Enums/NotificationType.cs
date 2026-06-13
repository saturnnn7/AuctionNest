namespace AuctionNest.Domain.Enums;

public enum NotificationType
{
    BidOutbid            = 0,  // You were outbid
    AuctionWon           = 1,  // You won the auction
    AuctionEndedSeller   = 2,  // Auction ended (seller notification)
    ReserveMet           = 3,  // Reserve price met (for seller)
    WatchedAuctionEnding = 4,  // Watched auction ending in <5 min
    BuyItNowPurchased    = 5   // Someone bought your item instantly
}