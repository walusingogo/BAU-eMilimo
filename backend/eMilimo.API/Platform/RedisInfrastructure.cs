using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eMilimo.API.Platform;

public sealed class RedisSettings
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
    public string InstanceName { get; set; } = "emilimo:";
    public int DefaultTtlMinutes { get; set; } = 15;
}

public sealed class RedisHealthCheck(IDistributedCache cache, RedisSettings settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled) return HealthCheckResult.Healthy("Redis is disabled; the local distributed-memory fallback is active.");
        var key = $"health:{Guid.NewGuid():N}";
        try
        {
            await cache.SetStringAsync(key, "ok", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
            }, cancellationToken);
            var value = await cache.GetStringAsync(key, cancellationToken);
            await cache.RemoveAsync(key, cancellationToken);
            return value == "ok"
                ? HealthCheckResult.Healthy("Redis is available.")
                : HealthCheckResult.Unhealthy("Redis did not return the readiness probe value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is enabled but unavailable.", ex);
        }
    }
}
