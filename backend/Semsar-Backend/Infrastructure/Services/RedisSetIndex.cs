using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

internal class RedisSetIndex
{
    private readonly IDatabase? _db;
    private readonly IDistributedCache _cache;
    private readonly string _indexKey;
    private readonly DistributedCacheEntryOptions _ttl;
    private readonly bool _useAtomic;

    public RedisSetIndex(IDistributedCache cache, string indexKey, DistributedCacheEntryOptions ttl, IConnectionMultiplexer? muxer = null)
    {
        _cache = cache;
        _indexKey = indexKey;
        _ttl = ttl;
        if (muxer != null)
        {
            _db = muxer.GetDatabase();
            _useAtomic = true;
        }
    }

    public async Task AddAsync(string item)
    {
        if (_useAtomic)
        {
            await _db!.SetAddAsync(new RedisKey(_indexKey), item);
        }
        else
        {
            var json = await _cache.GetStringAsync(_indexKey);
            var set = json is not null ? JsonSerializer.Deserialize<HashSet<string>>(json) ?? new() : new();
            set.Add(item);
            await _cache.SetStringAsync(_indexKey, JsonSerializer.Serialize(set), _ttl);
        }
    }

    public async Task<List<string>> GetAllAsync()
    {
        if (_useAtomic)
        {
            var members = await _db!.SetMembersAsync(new RedisKey(_indexKey));
            return members.Select(m => (string)m!).ToList();
        }
        var json = await _cache.GetStringAsync(_indexKey);
        return json is not null
            ? JsonSerializer.Deserialize<HashSet<string>>(json)?.ToList() ?? new()
            : new List<string>();
    }

    public async Task<HashSet<string>> GetSetAsync()
    {
        if (_useAtomic)
        {
            var members = await _db!.SetMembersAsync(new RedisKey(_indexKey));
            return new HashSet<string>(members.Select(m => (string)m!));
        }
        var json = await _cache.GetStringAsync(_indexKey);
        return json is not null
            ? JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>()
            : new HashSet<string>();
    }
}
