namespace AuctionNest.Application.Features.Auth.Common;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Username,
    string DisplayName,
    string Role);