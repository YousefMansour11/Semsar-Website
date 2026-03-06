using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace API.Services;

public class BotBehaviorDetector
{
    private readonly ILogger<BotBehaviorDetector> _logger;
    private readonly IBotBehaviorStore _store;

    private const int MaxRequestsPerVelocityWindow = 3;
    private static readonly TimeSpan VelocityWindow = TimeSpan.FromSeconds(30);
    private const double SimilarityThreshold = 0.75;
    private const int MaxPayloadsPerIp = 20;

    private static readonly TimeSpan ReputationTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan SeriousReputationTtl = TimeSpan.FromMinutes(60);

    public BotBehaviorDetector(ILogger<BotBehaviorDetector> logger, IBotBehaviorStore store)
    {
        _logger = logger;
        _store = store;
    }

    public string ComputeFingerprint(HttpContext context)
    {
        var ua = context.Request.Headers.UserAgent.FirstOrDefault() ?? "";
        var acceptLang = context.Request.Headers.AcceptLanguage.FirstOrDefault() ?? "";
        var secChUa = context.Request.Headers["Sec-CH-UA"].FirstOrDefault() ?? "";
        var raw = $"{ua}|{acceptLang}|{secChUa}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool CheckFingerprintConsistency(string ip, string fingerprint)
    {
        var consistent = _store.CheckAndStoreFingerprint(ip, fingerprint);
        if (!consistent)
        {
            _logger.LogWarning("AbuseAudit: Fingerprint mismatch IP={IP}", ip);
        }
        return consistent;
    }

    public bool IsDuplicatePayload(string ip, string body, out double similarity)
    {
        similarity = 0;
        var fp = ComputePayloadHash(body);
        var history = _store.GetPayloadHashes(ip);

        foreach (var existing in history)
        {
            var sim = ComputeSimilarity(fp, existing);
            if (sim > similarity)
                similarity = sim;

            if (sim >= SimilarityThreshold)
            {
                _logger.LogWarning("AbuseAudit: Duplicate payload detected IP={IP} Similarity={Sim:P1}", ip, sim);
                return true;
            }
        }

        _store.RecordPayloadHash(ip, fp);
        _store.TrimPayloadHistory(ip, MaxPayloadsPerIp);
        return false;
    }

    public bool IsVelocityExceeded(string ip)
    {
        var allowed = _store.CheckVelocity(ip, MaxRequestsPerVelocityWindow, VelocityWindow);
        if (!allowed)
        {
            _logger.LogWarning("AbuseAudit: Velocity exceeded IP={IP} Window={Window}s",
                ip, VelocityWindow.TotalSeconds);
        }
        return !allowed;
    }

    public bool IsSessionVelocityExceeded(string ip, string fingerprint)
    {
        var sessionKey = $"{ip}|{fingerprint}";
        var allowed = _store.CheckVelocity(sessionKey, MaxRequestsPerVelocityWindow, VelocityWindow);
        if (!allowed)
        {
            _logger.LogWarning("AbuseAudit: Session velocity exceeded Key={Key} Window={Window}s",
                sessionKey, VelocityWindow.TotalSeconds);
        }
        return !allowed;
    }

    public bool IsEntityVelocityExceeded(string ip, string entityType, string entityId, int maxRequests, TimeSpan window)
    {
        var allowed = _store.CheckEntityVelocity(ip, entityType, entityId, maxRequests, window);
        if (!allowed)
        {
            _logger.LogWarning("AbuseAudit: Entity velocity exceeded IP={IP} Type={Type} Id={Id} Max={Max} Window={Window}s",
                ip, entityType, entityId, maxRequests, window.TotalSeconds);
        }
        return !allowed;
    }

    public int ComputeReputationScore(string ip, string fingerprint)
    {
        var key = BuildRepKey(ip, fingerprint);
        return _store.GetReputationScore(key);
    }

    public void AddReputationEvent(string ip, string fingerprint, int delta, bool serious = false)
    {
        var key = BuildRepKey(ip, fingerprint);
        var ttl = serious ? SeriousReputationTtl : ReputationTtl;
        var newScore = _store.AddReputationScore(key, delta, ttl);
        _logger.LogInformation("AbuseAudit: Reputation score changed IP={IP} Delta={Delta} NewScore={NewScore}", ip, delta, newScore);
    }

    public bool IsInCooldown(string ip, string fingerprint, out int retryAfterSeconds)
    {
        var key = BuildRepKey(ip, fingerprint);
        return _store.TryGetCooldown(key, out retryAfterSeconds);
    }

    public void ApplyCooldown(string ip, string fingerprint, int durationSeconds)
    {
        var key = BuildRepKey(ip, fingerprint);
        _store.SetCooldown(key, durationSeconds);
        _logger.LogInformation("AbuseAudit: Cooldown applied IP={IP} Duration={Duration}s", ip, durationSeconds);
    }

    public (bool isDuplicate, double similarity, bool isRapidRepeat) AnalyzeEntityVelocity(
        string ip, string entityType, string entityId, string normalizedBody, out int entityRequestCount)
    {
        entityRequestCount = 0;

        var isDuplicate = IsDuplicatePayload(ip, normalizedBody, out var similarity);

        var velocityAllowed = _store.CheckEntityVelocity(ip, entityType, entityId, 2, TimeSpan.FromMinutes(15));
        var isRapidRepeat = !velocityAllowed;

        return (isDuplicate, similarity, isRapidRepeat);
    }

    public int CalculateScore(HttpContext context, string body, string entityType, string entityId,
        string ip, string fingerprint, bool honeypotFilled, bool malformedJson, bool hasViolations)
    {
        var score = 0;

        if (honeypotFilled)
            score += 20;

        if (malformedJson)
            score += 10;

        if (hasViolations)
            score += 10;

        var duplicate = IsDuplicatePayload(ip, body, out var similarity);
        if (duplicate)
            score += 10;

        if (similarity > 0.5 && similarity < 0.75)
            score += 5;

        var velocityExceeded = IsVelocityExceeded(ip);
        if (velocityExceeded)
            score += 20;

        var sessionVelocityExceeded = IsSessionVelocityExceeded(ip, fingerprint);
        if (sessionVelocityExceeded)
            score += 10;

        var entityVelocityExceeded = _store.CheckEntityVelocity(ip, entityType, entityId, 2, TimeSpan.FromMinutes(15));
        if (!entityVelocityExceeded)
            score += 15;

        if (!CheckFingerprintConsistency(ip, fingerprint))
            score += 15;

        var existingScore = ComputeReputationScore(ip, fingerprint);
        if (existingScore >= 40)
            score += existingScore / 2;

        return Math.Clamp(score, 0, 100);
    }

    public void PerformCleanup()
    {
        _store.Cleanup();
    }

    private static string BuildRepKey(string ip, string fingerprint)
    {
        return $"{ip}|{fingerprint}";
    }

    private static string ComputePayloadHash(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var normalized = NormalizePayload(doc.RootElement);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
        catch
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    private static string NormalizePayload(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element.GetRawText();

        var fields = new List<string>();
        foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            var name = prop.Name.ToLowerInvariant();

            if (name is "submittedat" or "interactiontimestamp" or "honeypot" or "firstvisitat" or "useragent")
                continue;

            if (name.StartsWith("hp_", StringComparison.OrdinalIgnoreCase))
                continue;

            if (prop.Value.ValueKind == JsonValueKind.String)
                fields.Add($"{name}={prop.Value.GetString()?.Trim().ToLowerInvariant()}");
        }

        return string.Join("&", fields);
    }

    private static double ComputeSimilarity(string a, string b)
    {
        if (a == b) return 1.0;

        int maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return 1.0;

        int distance = LevenshteinDistance(a.AsSpan(), b.AsSpan());
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        Span<int> previous = stackalloc int[b.Length + 1];
        Span<int> current = stackalloc int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (int i = 0; i < a.Length; i++)
        {
            current[0] = i + 1;

            for (int j = 0; j < b.Length; j++)
            {
                int cost = a[i] == b[j] ? 0 : 1;
                current[j + 1] = Math.Min(
                    Math.Min(current[j] + 1, previous[j + 1] + 1),
                    previous[j] + cost);
            }

            var temp = previous;
            previous = current;
            current = temp;
        }

        return previous[b.Length];
    }
}
