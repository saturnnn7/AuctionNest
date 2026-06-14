using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Features.Users.Common;

public sealed record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string Role,
    bool IsVerified,
    DateTime CreatedAt)
{
    public static UserProfileDto FromEntity(User u)
        => new(u.Id, u.Username, u.Email, u.DisplayName,
               u.Role.ToString(), u.IsVerified, u.CreatedAt);
}