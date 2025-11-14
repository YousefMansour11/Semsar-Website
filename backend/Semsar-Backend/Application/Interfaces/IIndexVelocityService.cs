namespace Application.Interfaces;

public class IndexVelocityResult
{
    public double CurrentVelocity { get; set; }
    public double TargetVelocity { get; set; }
    public int PagesIndexedToday { get; set; }
    public int PagesSubmittedToday { get; set; }
    public List<string> UrlsToPrioritize { get; set; } = new();
}

public interface IIndexVelocityService
{
    Task RecordSubmissionAsync(string url);
    Task RecordIndexingAsync(string url);
    Task<IndexVelocityResult> GetCurrentVelocityAsync();
    Task<List<string>> GetUrlsNeedingIndexingAsync(int maxCount = 50);
    Task<bool> ShouldSubmitToIndexNowAsync();
}
