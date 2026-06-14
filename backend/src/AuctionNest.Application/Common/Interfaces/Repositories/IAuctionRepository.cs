using AuctionNest.Application.Common.Models;
using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Common.Interfaces.Repositories;

public interface IAuctionRepository : IRepository<Auction>
{
    Task<(IReadOnlyList<Auction> Items, int TotalCount)> GetPagedAsync(
        AuctionFilterParams filter, CancellationToken ct = default);

    // Loads auction + all bids — used in PlaceBid to check IsWinning, anti-snipe
    Task<Auction?> GetWithBidsAsync(Guid id, CancellationToken ct = default);

    // Used by Hangfire worker to find auctions that need to end
    Task<List<Auction>> GetActiveEndedBeforeAsync(DateTime threshold, CancellationToken ct = default);

    // Used by WatchList notification worker (auctions ending in N minutes)
    Task<List<Auction>> GetEndingSoonAsync(TimeSpan window, CancellationToken ct = default);
}