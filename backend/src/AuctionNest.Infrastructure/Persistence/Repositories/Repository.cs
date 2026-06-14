using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public abstract class Repository<TEntity> : IRepository<TEntity>
    where TEntity : Entity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected Repository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await DbSet.AddAsync(entity, ct);

    public void Update(TEntity entity)
        => DbSet.Update(entity);

    public void Remove(TEntity entity)
        => DbSet.Remove(entity);
}