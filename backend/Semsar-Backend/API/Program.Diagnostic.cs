using System.Diagnostics;
using System.Reflection;
using System.Text;

[assembly: System.Runtime.CompilerServices.CompilerGenerated]

namespace API;

/// <summary>
/// Emergency startup diagnostic — logs ALL startup exceptions to startup-fatal.log
/// BEFORE IIS returns 500.0. Works even when Serilog or DI is not initialized.
/// Remove after root cause is fixed.
/// </summary>
internal static class StartupDiagnostic
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "startup-fatal.log");

    private static readonly object Lock = new();

    public static void WriteFatal(string phase, Exception ex, IConfiguration? config = null, IWebHostEnvironment? env = null)
    {
        try
        {
            lock (Lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine($"STARTUP FAILED at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z");
                sb.AppendLine($"Phase: {phase}");
                sb.AppendLine($"Machine: {Environment.MachineName}");
                sb.AppendLine($"Process: {Environment.ProcessPath}");
                sb.AppendLine($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                sb.AppendLine($"CurrentDirectory: {Environment.CurrentDirectory}");
                sb.AppendLine($"CommandLine: {Environment.CommandLine}");
                sb.AppendLine($"OS: {Environment.OSVersion}");
                sb.AppendLine($"CLR: {Environment.Version}");
                sb.AppendLine($"User: {Environment.UserName}");
                sb.AppendLine();

                // Exception chain
                sb.AppendLine("--- EXCEPTION ---");
                var depth = 0;
                for (var e = ex; e != null; e = e.InnerException, depth++)
                {
                    sb.AppendLine($"  #{depth}: {e.GetType().FullName}");
                    sb.AppendLine($"  Message: {e.Message}");
                    if (!string.IsNullOrEmpty(e.StackTrace))
                        sb.AppendLine($"  StackTrace: {e.StackTrace}");
                    sb.AppendLine();
                }

                // Configuration state
                if (config != null)
                {
                    sb.AppendLine("--- CONFIGURATION STATE ---");
                    var keys = new[]
                    {
                        "ASPNETCORE_ENVIRONMENT",
                        "ConnectionStrings:DefaultConnection",
                        "ConnectionStrings:Redis",
                        "Jwt:Key",
                        "Jwt:Issuer",
                        "Jwt:Audience",
                        "Cloudinary:CloudName",
                        "Cloudinary:ApiKey",
                        "Cloudinary:ApiSecret",
                        "Smtp:Host",
                        "Smtp:From",
                        "Smtp:User",
                        "AppSettings:BaseUrl",
                        "Serilog:LogPath",
                        "Cors:AllowedOrigins",
                    };
                    foreach (var key in keys)
                    {
                        var val = config[key];
                        var display = string.IsNullOrWhiteSpace(val)
                            ? "(MISSING / EMPTY)"
                            : key.Contains("Key") || key.Contains("Secret") || key.Contains("Pass")
                                ? $"{val[..Math.Min(4, val.Length)]}...({val.Length} chars)"
                                : val;
                        sb.AppendLine($"  {key} = {display}");
                    }
                }

                // Environment variables
                sb.AppendLine("--- KEY ENVIRONMENT VARIABLES ---");
                var envKeys = new[]
                {
                    "ASPNETCORE_ENVIRONMENT",
                    "ASPNETCORE_DETAILEDERRORS",
                    "DOTNET_RUNNING_IN_CONTAINER",
                    "ConnectionStrings__DefaultConnection",
                    "ConnectionStrings__Redis",
                    "Jwt__Key",
                    "Jwt__Issuer",
                    "Jwt__Audience",
                    "Cloudinary__CloudName",
                    "Cloudinary__ApiKey",
                    "Cloudinary__ApiSecret",
                };
                foreach (var key in envKeys)
                {
                    var val = Environment.GetEnvironmentVariable(key);
                    var display = val == null
                        ? "(NOT SET)"
                        : string.IsNullOrWhiteSpace(val)
                            ? "(EMPTY)"
                            : key.Contains("Key") || key.Contains("Secret") || key.Contains("Pass")
                                ? $"{val[..Math.Min(4, val.Length)]}...({val.Length} chars)"
                                : val;
                    sb.AppendLine($"  {key} = {display}");
                }

                sb.AppendLine("========================================");
                sb.AppendLine();

                File.AppendAllText(LogPath, sb.ToString());
            }
        }
        catch
        {
            // Last resort — cannot even write diagnostic log
        }
    }
}
