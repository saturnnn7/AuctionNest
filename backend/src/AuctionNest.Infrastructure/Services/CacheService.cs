using System.Text.Json;
using AuctionNest.Application.Common.Interfaces.Services;
using StackExchange.Redis;

namespace AuctionNest.Infrastructure.Services;

public sealed class CacheService : ICacheService
{
    private readonly IDatabase _db;

    public CacheService(IConnectionMultiplexer redis)
        => _db = redis.GetDatabase();

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => await _db.KeyExistsAsync(key);
}