namespace API.Services;

public interface IBotBehaviorStore
{
    bool CheckVelocity(string key, int maxRequests, TimeSpan window);
    bool CheckAndStoreFingerprint(string ip, string fingerprint);
    List<string> GetPayloadHashes(string ip);
    void RecordPayloadHash(string ip, string hash);
    void TrimPayloadHistory(string ip, int maxEntries);
    void Cleanup();

    bool CheckEntityVelocity(string ip, string entityType, string entityId, int maxRequests, TimeSpan window);

    int AddReputationScore(string key, int delta, TimeSpan ttl);
    int GetReputationScore(string key);

    bool TryGetCooldown(string key, out int retryAfterSeconds);
    void SetCooldown(string key, int durationSeconds);
}
