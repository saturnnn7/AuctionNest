namespace AuctionNest.Application.Common.Interfaces.Services;

public interface IDistributedLockService
{
    // Returns null если лок не удалось получить (уже занят)
    Task<IDistributedLock?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default);
}

public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
}