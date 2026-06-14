using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Application.Common.Models;
using AuctionNest.Domain.Entities;
using AuctionNest.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public sealed class AuctionRepository : Repository<Auction>, IAuctionRepository
{
    public AuctionRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Auction> Items, int TotalCount)> GetPagedAsync(
        AuctionFilterParams filter, CancellationToken ct = default)
    {
        var query = DbSet
            .Include(a => a.Seller)
            .Include(a => a.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(a =>
                a.Title.Contains(filter.Search) ||
                a.Description.Contains(filter.Search));

        if (filter.CategoryId.HasValue)
            query = query.Where(a => a.CategoryId == filter.CategoryId);

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status);

        if (filter.MinPrice.HasValue)
            query = query.Where(a => a.CurrentPrice >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(a => a.CurrentPrice <= filter.MaxPrice);

        var totalCount = await query.CountAsync(ct);

        query = filter.SortBy.ToLowerInvariant() switch
        {
            "price" => filter.SortDescending
                ? query.OrderByDescending(a => a.CurrentPrice)
                : query.OrderBy(a => a.CurrentPrice),
            _ => filter.SortDescending
                ? query.OrderByDescending(a => a.EndsAt)
                : query.OrderBy(a => a.EndsAt)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Auction?> GetWithBidsAsync(Guid id, CancellationToken ct = default)
        => await DbSet
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<Auction>> GetActiveEndedBeforeAsync(
        DateTime threshold, CancellationToken ct = default)
        => await DbSet
            .Where(a =>
                (a.Status == AuctionStatus.Active || a.Status == AuctionStatus.Extending)
                && a.EndsAt <= threshold)
            .ToListAsync(ct);

    public async Task<List<Auction>> GetEndingSoonAsync(
        TimeSpan window, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.Add(window);
        return await DbSet
            .Include(a => a.WatchList)
            .Where(a => a.Status == AuctionStatus.Active && a.EndsAt <= threshold)
            .ToListAsync(ct);
    }
}