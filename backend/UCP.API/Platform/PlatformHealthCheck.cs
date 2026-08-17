using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using UCP.Infrastructure.Data;

namespace UCP.API.Platform;

public sealed class PlatformHealthCheck(IServiceScopeFactory scopes, IFileStorage storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Database.CanConnectAsync(cancellationToken)) return HealthCheckResult.Unhealthy("Database is unavailable.");
            _ = storage.GetPhysicalPath("health/probe");
            return HealthCheckResult.Healthy("Database and storage configuration are available.");
        }
        catch (Exception ex) { return HealthCheckResult.Unhealthy("A required platform dependency is unavailable.", ex); }
    }
}
