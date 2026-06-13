using AuctionNest.Domain.Common;

namespace AuctionNest.Domain.Entities;

public sealed class Category : Entity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public ICollection<Auction> Auctions { get; private set; } = [];

    public static Category Create(string name, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = name.Trim().ToLower().Replace(" ", "-"),
            Description = description?.Trim()
        };

    public void SoftDelete()
        => DeletedAt = DateTime.UtcNow;
}