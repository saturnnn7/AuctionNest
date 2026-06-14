using AuctionNest.Application.Common.Interfaces.Services;
using RedLockNet;
using RedLockNet.SERedis;

namespace AuctionNest.Infrastructure.Services;

public sealed class DistributedLockService : IDistributedLockService
{
    private readonly IDistributedLockFactory _factory;

    public DistributedLockService(IDistributedLockFactory factory)
        => _factory = factory;

    public async Task<IDistributedLock?> AcquireAsync(
        string resource, TimeSpan expiry, CancellationToken ct = default)
    {
        var redLock = await _factory.CreateLockAsync(resource, expiry);

        if (!redLock.IsAcquired)
        {
            await redLock.DisposeAsync();
            return null;
        }

        return new RedLockWrapper(redLock);
    }
}

internal sealed class RedLockWrapper : IDistributedLock
{
    private readonly IRedLock _redLock;

    public RedLockWrapper(IRedLock redLock) => _redLock = redLock;

    public bool IsAcquired => _redLock.IsAcquired;

    public async ValueTask DisposeAsync() => await _redLock.DisposeAsync();
}