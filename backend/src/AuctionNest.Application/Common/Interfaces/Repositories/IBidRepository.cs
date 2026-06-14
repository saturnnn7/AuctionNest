using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Common.Interfaces.Repositories;

public interface IBidRepository : IRepository<Bid>
{
    Task<List<Bid>> GetByAuctionIdAsync(Guid auctionId, CancellationToken ct = default);
    Task<Bid?> GetWinningBidAsync(Guid auctionId, CancellationToken ct = default);
}