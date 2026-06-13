using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Errors;

public static class BidErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Bid.NotFound", "Bid was not found.");

    public static readonly Error DuplicateRequest =
        Error.Conflict("Bid.DuplicateRequest",
            "This bid request was already processed. Check idempotency key.");
}