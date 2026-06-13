using AuctionNest.Domain.Common;
using AuctionNest.Domain.Enums;

namespace AuctionNest.Domain.Entities;

public sealed class User : Entity
{
    public string Username { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public ICollection<Auction> Auctions { get; private set; } = [];
    public ICollection<Bid> Bids { get; private set; } = [];
    public ICollection<WatchList> WatchList { get; private set; } = [];
    public ICollection<Notification> Notifications { get; private set; } = [];

    public static User Create(
        string username,
        string email,
        string passwordHash,
        string displayName)
        => new()
        {
            Id = Guid.NewGuid(),
            Username = username.Trim().ToLower(),
            Email = email.Trim().ToLower(),
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim(),
            Role = UserRole.User,
            IsVerified = false
        };

    public void UpdatePasswordHash(string newHash)
        => PasswordHash = newHash;

    public void SoftDelete()
        => DeletedAt = DateTime.UtcNow;
}