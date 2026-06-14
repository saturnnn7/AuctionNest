using AuctionNest.Application.Common.Interfaces.Repositories;

namespace AuctionNest.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IAuctionRepository Auctions { get; }
    IUserRepository Users { get; }
    IBidRepository Bids { get; }
    ICategoryRepository Categories { get; }
    IWatchListRepository WatchLists { get; }
    INotificationRepository Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}