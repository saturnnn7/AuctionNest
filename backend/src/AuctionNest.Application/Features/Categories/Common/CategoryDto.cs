using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Features.Categories.Common;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description)
{
    public static CategoryDto FromEntity(Category c)
        => new(c.Id, c.Name, c.Slug, c.Description);
}