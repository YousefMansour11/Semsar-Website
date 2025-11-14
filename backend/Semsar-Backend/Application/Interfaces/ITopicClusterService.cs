namespace Application.Interfaces;

public class TopicCluster
{
    public string ClusterId { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string PillarPageUrl { get; set; } = string.Empty;
    public List<string> ClusterUrls { get; set; } = new();
    public string TopicKeyword { get; set; } = string.Empty;
    public List<string> RelatedKeywords { get; set; } = new();
    public double AuthorityScore { get; set; }
    public bool IsComplete { get; set; }
    public List<string> MissingTopics { get; set; } = new();
}

public class ClusterIntegrityResult
{
    public string ClusterId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int ExpectedPages { get; set; }
    public int ActualPages { get; set; }
    public double CompletenessRatio { get; set; }
    public List<string> Gaps { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public interface ITopicClusterService
{
    Task<TopicCluster> CreateClusterAsync(string topicName, string pillarPageUrl, string topicKeyword);
    Task AddToClusterAsync(string clusterId, string url, string keyword);
    Task<ClusterIntegrityResult> VerifyClusterIntegrityAsync(string clusterId);
    Task<List<TopicCluster>> GetAllClustersAsync();
    Task<List<string>> DetectContentGapsAsync(string clusterId);
    string BuildClusterJsonLd(TopicCluster cluster);
}
