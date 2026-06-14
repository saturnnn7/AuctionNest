namespace AuctionNest.Application.Common.Interfaces.Services;

// Abstraction over DateTime.UtcNow - needed to mock time in unit tests
// (anti-snipe validation, token expiry checks, etc.)
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}