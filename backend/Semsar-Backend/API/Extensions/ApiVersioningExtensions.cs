namespace API.Extensions;

public static class ApiVersioningExtensions
{
    public static IServiceCollection ConfigureApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = Microsoft.AspNetCore.Mvc.Versioning.ApiVersionReader.Combine(
                new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader(),
                new Microsoft.AspNetCore.Mvc.Versioning.QueryStringApiVersionReader());
        });

        return services;
    }
}
