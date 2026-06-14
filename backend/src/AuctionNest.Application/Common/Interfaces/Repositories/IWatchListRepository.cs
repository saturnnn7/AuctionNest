using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Common.Interfaces.Repositories;

public interface IWatchListRepository : IRepository<WatchList>
{
    Task<WatchList?> GetByUserAndAuctionAsync(
        Guid userId, Guid auctionId, CancellationToken ct = default);

    Task<List<WatchList>> GetByAuctionIdAsync(
        Guid auctionId, CancellationToken ct = default);
}