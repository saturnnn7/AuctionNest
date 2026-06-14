using AuctionNest.Domain.Entities;

namespace AuctionNest.Application.Common.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<List<Notification>> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(Guid userId, CancellationToken ct = default);
}