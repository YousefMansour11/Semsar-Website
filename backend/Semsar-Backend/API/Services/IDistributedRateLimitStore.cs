namespace API.Services;

public interface IDistributedRateLimitStore
{
    Task<bool> CheckAndIncrementAsync(string key, int maxRequests, TimeSpan window, CancellationToken ct = default);
    Task<long> GetCurrentCountAsync(string key, CancellationToken ct = default);
    Task ResetAsync(string key, CancellationToken ct = default);
}
