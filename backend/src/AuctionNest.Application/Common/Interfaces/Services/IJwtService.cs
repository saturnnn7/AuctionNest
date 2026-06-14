using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Common.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? ValidateAccessToken(string token);
}