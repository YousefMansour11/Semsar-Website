using System.Text.Json;
using Application.Interfaces;

namespace Application.Services;

public class TopicClusterService : ITopicClusterService
{
    private readonly List<TopicCluster> _clusters = new();
    private readonly object _lock = new();

    public Task<TopicCluster> CreateClusterAsync(string topicName, string pillarPageUrl, string topicKeyword)
    {
        var cluster = new TopicCluster
        {
            ClusterId = Guid.NewGuid().ToString("N")[..12],
            TopicName = topicName,
            PillarPageUrl = pillarPageUrl,
            TopicKeyword = topicKeyword,
            ClusterUrls = new List<string> { pillarPageUrl },
            RelatedKeywords = GenerateRelatedKeywords(topicKeyword),
            IsComplete = false,
            MissingTopics = new List<string>()
        };

        lock (_lock)
        {
            _clusters.Add(cluster);
        }

        return Task.FromResult(cluster);
    }

    public Task AddToClusterAsync(string clusterId, string url, string keyword)
    {
        lock (_lock)
        {
            var cluster = _clusters.FirstOrDefault(c => c.ClusterId == clusterId);
            if (cluster != null)
            {
                if (!cluster.ClusterUrls.Contains(url))
                    cluster.ClusterUrls.Add(url);
                if (!cluster.RelatedKeywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
                    cluster.RelatedKeywords.Add(keyword);
            }
        }

        return Task.CompletedTask;
    }

    public Task<ClusterIntegrityResult> VerifyClusterIntegrityAsync(string clusterId)
    {
        lock (_lock)
        {
            var cluster = _clusters.FirstOrDefault(c => c.ClusterId == clusterId);
            if (cluster == null)
                return Task.FromResult(new ClusterIntegrityResult
                {
                    ClusterId = clusterId,
                    IsValid = false,
                    Gaps = new List<string> { "Cluster not found" }
                });

            var gaps = DetectContentGapsInternal(cluster);
            var expected = cluster.RelatedKeywords.Count;
            var actual = cluster.ClusterUrls.Count;

            return Task.FromResult(new ClusterIntegrityResult
            {
                ClusterId = clusterId,
                IsValid = gaps.Count == 0,
                ExpectedPages = expected,
                ActualPages = actual,
                CompletenessRatio = expected > 0 ? (double)actual / expected : 0,
                Gaps = gaps,
                Recommendations = gaps.Select(g => $"Create content targeting: {g}").ToList()
            });
        }
    }

    public Task<List<TopicCluster>> GetAllClustersAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(new List<TopicCluster>(_clusters));
        }
    }

    public Task<List<string>> DetectContentGapsAsync(string clusterId)
    {
        lock (_lock)
        {
            var cluster = _clusters.FirstOrDefault(c => c.ClusterId == clusterId);
            return Task.FromResult(cluster != null ? DetectContentGapsInternal(cluster) : new List<string>());
        }
    }

    public string BuildClusterJsonLd(TopicCluster cluster)
    {
        try
        {
            var itemList = new List<object>();
            for (int i = 0; i < cluster.ClusterUrls.Count; i++)
            {
                itemList.Add(new Dictionary<string, object?>
                {
                    ["@type"] = "ListItem",
                    ["position"] = i + 1,
                    ["url"] = cluster.ClusterUrls[i]
                });
            }

            var obj = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "ItemList",
                ["name"] = cluster.TopicName,
                ["description"] = $"Topic cluster: {cluster.TopicName}",
                ["numberOfItems"] = cluster.ClusterUrls.Count,
                ["itemListOrder"] = "https://schema.org/ItemListOrderAscending",
                ["itemListElement"] = itemList
            };

            return JsonSerializer.Serialize(obj);
        }
        catch
        {
            return string.Empty;
        }
    }

    private List<string> DetectContentGapsInternal(TopicCluster cluster)
    {
        var gaps = new List<string>();

        foreach (var keyword in cluster.RelatedKeywords)
        {
            bool hasContent = cluster.ClusterUrls.Any() || cluster.TopicKeyword.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            if (!hasContent)
                gaps.Add(keyword);
        }

        return gaps;
    }

    private static List<string> GenerateRelatedKeywords(string primaryKeyword)
    {
        var keywords = new List<string>
        {
            primaryKeyword,
            $"best {primaryKeyword}",
            $"top {primaryKeyword}",
            $"affordable {primaryKeyword}",
            $"luxury {primaryKeyword}"
        };

        return keywords;
    }
}
