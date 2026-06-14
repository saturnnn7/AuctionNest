using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public sealed class WatchListRepository : Repository<WatchList>, IWatchListRepository
{
    public WatchListRepository(AppDbContext context) : base(context) { }

    public async Task<WatchList?> GetByUserAndAuctionAsync(
        Guid userId, Guid auctionId, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId, ct);

    public async Task<List<WatchList>> GetByAuctionIdAsync(
        Guid auctionId, CancellationToken ct = default)
        => await DbSet
            .Where(w => w.AuctionId == auctionId)
            .ToListAsync(ct);
    
    public async Task<List<WatchList>> GetByUserIdWithAuctionsAsync(
        Guid userId, CancellationToken ct = default)
        => await DbSet
            .Include(w => w.Auction)
                .ThenInclude(a => a.Seller)
            .Include(w => w.Auction)
                .ThenInclude(a => a.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
}