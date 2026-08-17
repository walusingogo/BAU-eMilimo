namespace UCP.API.Platform;

public sealed record StoredFile(string Key, long Size, string ContentType);
public sealed record StoredFileInfo(string Key, long Size, DateTime LastModifiedUtc);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    bool Exists(string key);
    IReadOnlyList<StoredFileInfo> List(string prefix);
    string GetPhysicalPath(string key);
}

/// <summary>
/// Filesystem implementation. In clustered deployments BasePath must point to
/// the same mounted volume on every node. The interface can later be backed by
/// S3, MinIO or Azure Blob without changing business modules.
/// </summary>
public sealed class FileSystemStorage(IConfiguration configuration, IWebHostEnvironment environment) : IFileStorage
{
    private readonly string _root = ResolveRoot(configuration["FileStorage:SharedRoot"], environment.ContentRootPath);

    public async Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(target, cancellationToken);
        return new StoredFile(Normalize(key), target.Length, contentType);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public bool Exists(string key) => File.Exists(Resolve(key));
    public IReadOnlyList<StoredFileInfo> List(string prefix)
    {
        var folder = Resolve(prefix.TrimEnd('/') + "/_");
        folder = Path.GetDirectoryName(folder)!;
        if (!Directory.Exists(folder)) return [];
        return new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories)
            .Select(x => new StoredFileInfo(Path.GetRelativePath(_root, x.FullName).Replace('\\', '/'), x.Length, x.LastWriteTimeUtc))
            .OrderByDescending(x => x.LastModifiedUtc).ToArray();
    }
    public string GetPhysicalPath(string key) => Resolve(key);

    private string Resolve(string key)
    {
        var normalized = Normalize(key);
        var path = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Invalid storage key.", nameof(key));
        return path;
    }

    private static string Normalize(string key)
    {
        var normalized = key.Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Split('/').Any(x => x is "" or "." or ".."))
            throw new ArgumentException("Invalid storage key.", nameof(key));
        return normalized;
    }

    private static string ResolveRoot(string? configured, string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(Path.Combine(contentRoot, "wwwroot", "uploads"));
        return Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(contentRoot, configured));
    }
}
