using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Errors;

public static class AuctionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Auction.NotFound", "Auction was not found.");

    public static readonly Error NotActive =
        Error.Conflict("Auction.NotActive", "Auction is not currently active.");

    public static readonly Error SellerCannotBid =
        Error.Forbidden("Auction.SellerCannotBid", "Seller cannot bid on their own auction.");

    public static Error BidTooLow(decimal minimum) =>
        Error.Validation("Auction.BidTooLow",
            $"Bid must be at least {minimum:F2}. Place a higher bid.");

    public static readonly Error BuyItNowNotAvailable =
        Error.Conflict("Auction.BuyItNowNotAvailable",
            "Buy It Now is not available — bids have already been placed.");

    public static readonly Error InvalidStatusTransition =
        Error.Conflict("Auction.InvalidStatusTransition",
            "Cannot perform this action in the current auction state.");

    public static readonly Error CannotCancelEnded =
        Error.Conflict("Auction.CannotCancelEnded",
            "An ended auction cannot be cancelled.");

    public static readonly Error NotOwner =
        Error.Forbidden("Auction.NotOwner",
            "Only the auction owner can perform this action.");
}