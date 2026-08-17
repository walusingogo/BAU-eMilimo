namespace UCP.API.Platform;

public sealed record PlatformCapability(string Key, string Name, string Permission);

public sealed record PlatformModule(
    string Key,
    string Name,
    string Description,
    string Route,
    int Order,
    bool Enabled,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<PlatformCapability> Capabilities);

public interface IPlatformModuleCatalog
{
    IReadOnlyList<PlatformModule> GetModules();
    PlatformModule? Find(string key);
}

public sealed class ConfigurationPlatformModuleCatalog(IConfiguration configuration) : IPlatformModuleCatalog
{
    private readonly IReadOnlyList<PlatformModule> _modules = Load(configuration);

    public IReadOnlyList<PlatformModule> GetModules() => _modules;

    public PlatformModule? Find(string key) =>
        _modules.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<PlatformModule> Load(IConfiguration configuration)
    {
        var modules = configuration.GetSection("Platform:Modules").Get<List<PlatformModule>>() ?? [];
        var duplicate = modules.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
        if (duplicate != null) throw new InvalidOperationException($"Duplicate platform module key '{duplicate.Key}'.");
        return modules.OrderBy(x => x.Order).ThenBy(x => x.Name).ToArray();
    }
}
