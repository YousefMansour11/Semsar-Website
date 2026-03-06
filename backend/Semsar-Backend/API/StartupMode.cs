namespace API;

public enum StartupMode
{
    Normal,
    Safe,
    Diagnostics
}

public static class StartupModeProvider
{
    private static StartupMode? _cached;

    public static StartupMode GetCurrent()
    {
        if (_cached.HasValue) return _cached.Value;

        var env = Environment.GetEnvironmentVariable("SEM_SAR_STARTUP_MODE") ?? "";
        _cached = env switch
        {
            "Safe" or "safe" => StartupMode.Safe,
            "Diagnostics" or "diagnostics" => StartupMode.Diagnostics,
            _ => StartupMode.Normal
        };
        return _cached.Value;
    }

    public static bool IsDiagnostics => GetCurrent() == StartupMode.Diagnostics;
    public static bool IsSafe => GetCurrent() == StartupMode.Safe;
    public static bool IsNormal => GetCurrent() == StartupMode.Normal;
}
