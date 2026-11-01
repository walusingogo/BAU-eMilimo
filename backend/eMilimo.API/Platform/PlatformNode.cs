using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Platform;

public sealed record PlatformNode(string NodeId, string HostName, string Environment, string Product, string Instance);

public interface IPlatformNode
{
    PlatformNode Current { get; }
    Task<string> GetDeploymentIdentityAsync(CancellationToken cancellationToken = default);
}

public sealed class PlatformNodeService(IConfiguration configuration, IWebHostEnvironment environment, IServiceScopeFactory scopes) : IPlatformNode
{
    public PlatformNode Current { get; } = CreateNode(configuration, environment);

    public async Task<string> GetDeploymentIdentityAsync(CancellationToken cancellationToken = default)
    {
        var configured = configuration["Platform:DeploymentId"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim();

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.SystemConfigs.AsNoTracking()
            .Where(x => !x.IsArchived && x.ConfigKey == "platform_deployment_id")
            .Select(x => x.ConfigValue).FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(stored) ? Current.Instance : stored;
    }

    private static PlatformNode CreateNode(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var host = Environment.MachineName;
        var instance = configuration["Platform:Instance"] ?? "default";
        var raw = $"{host}|{environment.EnvironmentName}|{instance}";
        var nodeId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..12].ToLowerInvariant();
        return new PlatformNode(nodeId, host, environment.EnvironmentName,
            configuration["Platform:Product"] ?? "eMilimo", instance);
    }
}
