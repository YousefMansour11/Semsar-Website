using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class SlugService : ISlugService
    {
        private readonly ILogger<SlugService>? _logger;

        public SlugService(ILogger<SlugService>? logger = null)
        {
            _logger = logger;
        }

        public string GenerateCandidateSlug(string titleEn, string? titleAr, string? location, string? preferredLang = null)
        {
            var baseText = !string.IsNullOrWhiteSpace(titleEn) ? titleEn : (titleAr ?? string.Empty);
            var input = (baseText ?? string.Empty) + " " + (location ?? string.Empty);
            var baseSlug = Slugify(input.Trim(), "en");
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = NormalizeSlug("item-" + ShortHash());
            }
            var deduplicated = DeduplicateSlugTokens(baseSlug);
            return deduplicated ?? string.Empty;
        }

        public string Slugify(string? input, string? lang = null)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input!.Trim();
            s = Regex.Replace(s, "\\s+", " ");
            s = s.Replace("\"", "").Replace("'", "").Replace("\\", "");
            s = s.ToLowerInvariant();
            s = Regex.Replace(s, "\\s+", "-");
            s = s.Normalize(NormalizationForm.FormD);
            s = Regex.Replace(s, "[^\u0000-\u007F]+", "");
            s = Regex.Replace(s, "[^a-z0-9-]+", "");
            s = Regex.Replace(s, "-{2,}", "-");
            try { _logger?.LogInformation("Slugify produced '{slug}' from input", s); } catch (Exception ex) { _logger?.LogWarning(ex, "Failed to log slugify result"); }
            return s.Trim('-');
        }

        public string NormalizeSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return string.Empty;
            var s = slug.Trim();
            s = s.ToLowerInvariant();
            s = Regex.Replace(s, "\\s+", "-");
            s = s.Normalize(NormalizationForm.FormD);
            s = Regex.Replace(s, "[^\u0000-\u007F]+", "");
            s = Regex.Replace(s, "[^a-z0-9-]+", "");
            s = Regex.Replace(s, "-{2,}", "-");
            return s.Trim('-');
        }

        public string DeduplicateSlugTokens(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return string.Empty;

            var tokens = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length <= 1) return slug;

            // First pass: remove consecutive duplicates
            var afterConsecutive = new List<string>();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (i == 0 || !string.Equals(tokens[i], tokens[i - 1], StringComparison.OrdinalIgnoreCase))
                {
                    afterConsecutive.Add(tokens[i]);
                }
            }

            // Second pass: remove non-consecutive duplicates (keep first occurrence)
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (var token in afterConsecutive)
            {
                if (seen.Add(token))
                {
                    result.Add(token);
                }
            }

            return string.Join("-", result);
        }

        private static string ShortHash()
        {
            byte[] bytes = new byte[6];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var sb = new StringBuilder(12);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString().Substring(0, 6);
        }
    }
}
