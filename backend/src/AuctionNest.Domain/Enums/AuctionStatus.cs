namespace AuctionNest.Domain.Enums;

public enum AuctionStatus
{
    Draft       = 0,
    Active      = 1,
    Extending    = 2, // Anti-snipe extension active
    Ended       = 3,
    Cancelled   = 4
}