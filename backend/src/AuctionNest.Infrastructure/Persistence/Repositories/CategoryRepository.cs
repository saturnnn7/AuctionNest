using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public sealed class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<List<Category>> GetAllActiveAsync(CancellationToken ct = default)
        => await DbSet.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await DbSet.AnyAsync(c => c.Id == id, ct);
}