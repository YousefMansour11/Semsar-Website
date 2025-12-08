using System.Net;
using System.Text;

namespace Application.Services;

public static class EmailTemplateService
{
    public static string Enc(string? value) => WebUtility.HtmlEncode(value ?? "");

    public static string BuildDocument(string heading, string bodyContent, DateTime timestamp)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"" dir=""ltr"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<meta name=""color-scheme"" content=""light"">
<meta name=""supported-color-schemes"" content=""light"">
<title>{Enc(heading)}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f4f5;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f4f4f5;"">
<tr><td align=""center"" style=""padding:32px 16px;"">
<table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;width:100%;"">

<tr>
<td style=""padding:28px 32px 20px;background-color:#0A1628;border-radius:16px 16px 0 0;text-align:center;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
<tr><td style=""font-size:22px;font-weight:700;letter-spacing:3px;color:#B5934A;"">SEMSAR</td></tr>
<tr><td style=""font-size:12px;color:#8899AA;padding-top:2px;"">Real Estate</td></tr>
</table>
</td>
</tr>

<tr>
<td style=""padding:24px 32px 0;background-color:#ffffff;"">
<h2 style=""margin:0;font-size:20px;font-weight:600;color:#1a1a2e;"">{Enc(heading)}</h2>
<hr style=""border:none;border-top:1px solid #e2e8f0;margin:16px 0 0;"">
</td>
</tr>

<tr>
<td style=""padding:24px 32px 32px;background-color:#ffffff;"">
{bodyContent}
</td>
</tr>

<tr>
<td style=""padding:20px 32px;background-color:#f8f9fa;border-radius:0 0 16px 16px;text-align:center;border-top:1px solid #e2e8f0;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
<tr><td style=""font-size:12px;color:#94a3b8;padding-bottom:4px;"">Received {timestamp:MMM dd, yyyy 'at' HH:mm} UTC</td></tr>
<tr><td style=""font-size:12px;color:#94a3b8;"">SEMSAR Real Estate</td></tr>
</table>
</td>
</tr>

</table>
</td></tr>
</table>
</body>
</html>";
    }

    public static string Card(string rows)
    {
        return $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f8f9fa;border-radius:12px;margin-bottom:20px;"">
<tr><td style=""padding:20px;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
{rows}
</table>
</td></tr>
</table>";
    }

    public static string Row(string label, string? value, bool isPhone = false, bool isMultiline = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return $@"<tr>
<td width=""35%"" style=""padding:6px 12px 6px 0;font-size:13px;color:#64748b;vertical-align:top;white-space:nowrap;"">{Enc(label)}</td>
<td width=""65%"" style=""padding:6px 0;font-size:14px;color:#94a3b8;"">—</td>
</tr>";
        }

        var displayValue = isPhone
            ? $"<a href=\"tel:{Enc(value.Replace(" ", "").Replace("\t", "").Replace("-", ""))}\" style=\"color:#0A1628;text-decoration:none;font-weight:500;\">{Enc(value)}</a>"
            : isMultiline
                ? $"<span dir=\"auto\" style=\"color:#0A1628;font-weight:500;white-space:pre-wrap;word-break:break-word;\">{Enc(value)}</span>"
                : $"<span dir=\"auto\" style=\"color:#0A1628;font-weight:500;\">{Enc(value)}</span>";

        return $@"<tr>
<td width=""35%"" style=""padding:6px 12px 6px 0;font-size:13px;color:#64748b;vertical-align:top;white-space:nowrap;"">{Enc(label)}</td>
<td width=""65%"" style=""padding:6px 0;font-size:14px;word-break:break-word;"">{displayValue}</td>
</tr>";
    }

    public static string Badge(string value)
    {
        return $"<span style=\"display:inline-block;padding:2px 10px;background-color:#e2e8f0;border-radius:999px;font-size:12px;color:#475569;font-weight:500;\">{Enc(value)}</span>";
    }

    public static string Divider()
    {
        return "<hr style=\"border:none;border-top:1px solid #e2e8f0;margin:0 0 16px;\">";
    }

    public static string SubHeading(string text)
    {
        return $"<p style=\"margin:0 0 12px;font-size:13px;font-weight:600;color:#64748b;text-transform:uppercase;letter-spacing:0.5px;\">{Enc(text)}</p>";
    }

    public static string TrackingSection(
        string? source, string? medium, string? campaign, string? term, string? content,
        string? landingPage, string? currentPage, string? referrer, string? userAgent,
        int pageViews, int? sessionDuration, string? lastReferrer)
    {
        var rows = new StringBuilder();

        void Add(string label, string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return;
            rows.Append(Row(label, val));
        }

        Add("Source", source);
        Add("Campaign", campaign);
        Add("Medium", medium);
        Add("Term", term);
        Add("Content", content);
        Add("Landing Page", landingPage);
        Add("Current Page", currentPage);
        Add("Referrer", referrer);
        Add("Last Referrer", lastReferrer);
        Add("User Agent", userAgent);
        Add("Page Views", pageViews > 0 ? pageViews.ToString() : null);
        if (sessionDuration.HasValue && sessionDuration.Value > 0)
            Add("Session Duration", FormatDuration(sessionDuration.Value));

        if (rows.Length == 0) return "";

        return $@"<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-bottom:0;"">
<tr><td style=""padding-top:4px;"">
{Divider()}
{SubHeading("Tracking")}
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
{rows}
</table>
</td></tr>
</table>";
    }

    private static string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds}s";
        if (seconds < 3600) return $"{seconds / 60}m {seconds % 60}s";
        return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
    }
}
