using System.Text.RegularExpressions;

namespace Application.Services;

public static partial class LeadNormalizer
{
    private static readonly Regex PhoneDigitsOnly = PhoneDigitsOnlyRegex();
    private static readonly Regex MultipleSpaces = MultipleSpacesRegex();
    private static readonly char[] TrimChars = [' ', '\t', '\n', '\r'];

    [GeneratedRegex(@"[^\d+]", RegexOptions.Compiled)]
    private static partial Regex PhoneDigitsOnlyRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    /// <summary>
    /// Normalize phone to E.164 format if possible; else strip to digits+.
    /// </summary>
    public static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var cleaned = PhoneDigitsOnly.Replace(phone, "").Trim();

        if (cleaned.Length == 0)
            return string.Empty;

        // Egyptian numbers: if 10 digits starting with 1, prepend +20
        if (cleaned.Length == 10 && cleaned[0] == '1')
            return "+20" + cleaned;

        // Egyptian numbers with 0 prefix: 01XXXXXXXXX → +20XXXXXXXXX
        if (cleaned.Length == 11 && cleaned[0] == '0' && cleaned[1] == '1')
            return "+2" + cleaned[1..];

        // Already has +
        if (cleaned[0] == '+')
            return cleaned;

        return "+" + cleaned;
    }

    /// <summary>
    /// Normalize text: trim, collapse whitespace, strip invisible chars.
    /// </summary>
    public static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var result = input.Trim(TrimChars);
        result = MultipleSpaces.Replace(result, " ");

        // Strip zero-width and invisible Unicode characters
        result = string.Create(result.Length, result, (span, s) =>
        {
            int write = 0;
            foreach (var c in s)
            {
                if (c is not ((char)0x200B or (char)0x200C or (char)0x200D or
                              (char)0xFEFF or (char)0x00AD or (char)0x2060 or
                              (char)0x2061 or (char)0x2062 or (char)0x2063 or
                              (char)0x2064))
                {
                    span[write++] = c;
                }
            }
            span = span[..write];
        });

        return result;
    }

    /// <summary>
    /// Check if two phone numbers match after normalization.
    /// </summary>
    public static bool PhonesMatch(string? a, string? b)
    {
        return string.Equals(NormalizePhone(a), NormalizePhone(b), StringComparison.Ordinal);
    }

    /// <summary>
    /// Basic email validation — returns normalized email or empty.
    /// </summary>
    public static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var trimmed = email.Trim().ToLowerInvariant();

        // Simple regex check
        if (trimmed.Contains('@') && trimmed.Contains('.') && trimmed.Length >= 5)
            return trimmed;

        return string.Empty;
    }

    /// <summary>
    /// Returns a stable hash for dedup checks (phone + message prefix).
    /// </summary>
    public static string DedupKey(string phone, string? message)
    {
        var normPhone = NormalizePhone(phone);
        var msgPrefix = NormalizeText(message ?? "");
        msgPrefix = msgPrefix.Length > 50 ? msgPrefix[..50] : msgPrefix;
        return $"{normPhone}:{msgPrefix}";
    }
}
