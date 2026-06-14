using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Errors;

public static class NotificationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Notification.NotFound", "Notification was not found.");

    public static readonly Error Forbidden =
        Error.Forbidden("Notification.Forbidden",
            "You do not have permission to access this notification.");
}