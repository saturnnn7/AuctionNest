using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public sealed class BidRepository : Repository<Bid>, IBidRepository
{
    public BidRepository(AppDbContext context) : base(context) { }

    public async Task<List<Bid>> GetByAuctionIdAsync(Guid auctionId, CancellationToken ct = default)
        => await DbSet
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .ToListAsync(ct);

    public async Task<Bid?> GetWinningBidAsync(Guid auctionId, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(b => b.AuctionId == auctionId && b.IsWinning, ct);
}