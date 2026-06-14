using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Errors;

public static class WatchListErrors
{
    public static readonly Error AlreadyWatching =
        Error.Conflict("WatchList.AlreadyWatching",
            "You are already watching this auction.");

    public static readonly Error NotWatching =
        Error.NotFound("WatchList.NotWatching",
            "This auction is not in your watch list.");
}