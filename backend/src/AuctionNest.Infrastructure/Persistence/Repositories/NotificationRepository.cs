using AuctionNest.Application.Common.Interfaces.Repositories;
using AuctionNest.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionNest.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<List<Notification>> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
        => await DbSet
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await DbSet.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
        => await DbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
}