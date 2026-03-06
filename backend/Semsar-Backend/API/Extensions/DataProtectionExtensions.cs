using Microsoft.AspNetCore.DataProtection;

namespace API.Extensions;

public static class DataProtectionExtensions
{
    public static IServiceCollection ConfigureDataProtection(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var keysPath = Path.Combine(environment.ContentRootPath, "..", "dataprotection-keys");
        keysPath = Path.GetFullPath(keysPath);

        try
        {
            if (!Directory.Exists(keysPath))
                Directory.CreateDirectory(keysPath);
        }
        catch
        {
            // Fall back to the app directory if we can't create the parent dir
            keysPath = Path.Combine(environment.ContentRootPath, "App_Data", "keys");
            if (!Directory.Exists(keysPath))
                Directory.CreateDirectory(keysPath);
        }

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("Semsar");

        return services;
    }
}
