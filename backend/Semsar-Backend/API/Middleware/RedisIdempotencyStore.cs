using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace API.Middleware
{
    public class RedisIdempotencyStore : IIdempotencyStore
    {
        private readonly IDatabase _db;
        private readonly string _prefix = "idempotency:";
        private readonly string _lockPrefix = "idempotency:lock:";

        public RedisIdempotencyStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<IdempotencyRecord?> GetAsync(string key)
        {
            var value = await _db.StringGetAsync(_prefix + key);
            if (value.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<IdempotencyRecord>((string)value!);
        }

        public async Task<bool> TryAcquireAsync(string key, TimeSpan lockTimeout)
        {
            var result = await _db.StringSetAsync(_lockPrefix + key, "1", lockTimeout, When.NotExists);
            return result;
        }

        public async Task StoreAsync(string key, IdempotencyRecord record, TimeSpan retention)
        {
            var json = JsonSerializer.Serialize(record);
            await _db.StringSetAsync(_prefix + key, json, retention > TimeSpan.Zero ? retention : TimeSpan.FromHours(24));
            await _db.KeyDeleteAsync(_lockPrefix + key);
        }

        public Task ReleaseLockAsync(string key)
        {
            return _db.KeyDeleteAsync(_lockPrefix + key);
        }

        public Task CleanupAsync(TimeSpan retention)
        {
            return Task.CompletedTask;
        }
    }
}
