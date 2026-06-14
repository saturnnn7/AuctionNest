namespace AuctionNest.Application.Common.Interfaces.Services;

public interface IDistributedLockService
{
    // Returns null if the lock could not be acquired (already held)
    Task<IDistributedLock?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default);
}

public interface IDistributedLock : IAsyncDisposable
{
    bool IsAcquired { get; }
}