using System.Text;
using System.Text.RegularExpressions;

namespace Application.Services;

public static class HtmlSanitizer
{
    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]*>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ScriptEventRegex = new(
        @"\bon\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ScriptTagRegex = new(
        @"<\s*/?script[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex DataUriRegex = new(
        @"data\s*:",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex JavascriptUriRegex = new(
        @"javascript\s*:",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string? Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var result = input;

        result = System.Net.WebUtility.HtmlDecode(result);

        var decodedAgain = System.Net.WebUtility.HtmlDecode(result);
        if (decodedAgain != result)
        {
            result = decodedAgain;
        }

        result = ScriptTagRegex.Replace(result, string.Empty);
        result = HtmlTagRegex.Replace(result, string.Empty);

        if (ScriptEventRegex.IsMatch(result) ||
            DataUriRegex.IsMatch(result) ||
            JavascriptUriRegex.IsMatch(result))
        {
            result = ScriptEventRegex.Replace(result, string.Empty);
            result = DataUriRegex.Replace(result, string.Empty);
            result = JavascriptUriRegex.Replace(result, string.Empty);
        }

        return result.Trim();
    }
}
