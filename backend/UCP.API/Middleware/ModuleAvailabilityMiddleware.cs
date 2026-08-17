using UCP.API.Platform;

namespace UCP.API.Middleware;

public sealed class ModuleAvailabilityMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ModuleRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "contributions", "requisitions", "assets", "reports"
    };

    public async Task InvokeAsync(HttpContext context, IPlatformModuleCatalog catalog)
    {
        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var key = segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase) ? segments[1] : null;
        if (key != null && ModuleRoutes.Contains(key) && catalog.Find(key) is { Enabled: false })
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = "This module is not available." });
            return;
        }
        await next(context);
    }
}
