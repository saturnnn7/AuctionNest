using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuctionNest.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        AppDbContext context,
        IAuctionRepository auctions,
        IUserRepository users,
        IBidRepository bids,
        ICategoryRepository categories,
        IWatchListRepository watchLists,
        INotificationRepository notifications)
    {
        _context = context;
        Auctions = auctions;
        Users = users;
        Bids = bids;
        Categories = categories;
        WatchLists = watchLists;
        Notifications = notifications;
    }

    public IAuctionRepository Auctions { get; }
    public IUserRepository Users { get; }
    public IBidRepository Bids { get; }
    public ICategoryRepository Categories { get; }
    public IWatchListRepository WatchLists { get; }
    public INotificationRepository Notifications { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
        await _transaction!.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        await _transaction!.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}