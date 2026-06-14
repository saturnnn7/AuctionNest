using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Common.Interfaces.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<List<Category>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}