using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "User was not found.");

    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("User.EmailAlreadyInUse", "This email is already registered.");

    public static readonly Error UsernameAlreadyInUse =
        Error.Conflict("User.UsernameAlreadyInUse", "This username is already taken.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "Invalid username or password.");

    public static readonly Error Unauthorized =
        Error.Unauthorized("User.Unauthorized", "Authentication is required.");

    public static readonly Error Forbidden =
        Error.Forbidden("User.Forbidden", "You do not have permission to perform this action.");
}