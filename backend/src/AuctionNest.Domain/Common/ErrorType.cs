namespace AuctionNest.Domain.Common;

/// <summary>
/// Represents the type of error that occurred.
/// </summary>
public enum ErrorType
{
    Failure         = 0, // domain or generic error
    Validation      = 1, // invalid input data
    NotFound        = 2, // requested resource not found
    Conflict        = 3, // resource already exists or state conflict (bid too low, already ended...)
    Unauthorized    = 4, // user is not authenticated
    Forbidden       = 5, // user is authenticated but lacks permission
    Internal        = 6  // infrastructure error (DB, Redis, external API...)
}