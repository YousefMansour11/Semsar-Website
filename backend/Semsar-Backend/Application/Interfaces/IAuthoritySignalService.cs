namespace Application.Interfaces;

public class AuthoritySignal
{
    public string Domain { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public double Score { get; set; }
    public DateTime LastChecked { get; set; }
    public string? Source { get; set; }
}

public class AuthorityScoreResult
{
    public string PageUrl { get; set; } = string.Empty;
    public double DomainAuthority { get; set; }
    public double PageAuthority { get; set; }
    public double TrustFlow { get; set; }
    public double CitationFlow { get; set; }
    public int ReferringDomains { get; set; }
    public int Backlinks { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public interface IAuthoritySignalService
{
    Task<AuthorityScoreResult> GetAuthorityScoreAsync(string url);
    Task RecordBacklinkAsync(string targetUrl, string sourceUrl);
    Task<double> CalculateEntityAuthorityAsync(string entityType, string slug);
    Task<List<string>> GetTopAuthorityPagesAsync(int count = 10);
}
