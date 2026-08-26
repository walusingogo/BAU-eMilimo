using System.Text.Json;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

public interface ISystemLogWriter
{
    Task WriteAsync(SystemLog entry, CancellationToken cancellationToken = default);
}

public class SystemLogWriter : ISystemLogWriter
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly IServiceScopeFactory _scopes;
    private readonly IWebHostEnvironment _environment;

    public SystemLogWriter(IServiceScopeFactory scopes, IWebHostEnvironment environment)
    {
        _scopes = scopes;
        _environment = environment;
    }

    public async Task WriteAsync(SystemLog entry, CancellationToken cancellationToken = default)
    {
        // 1. Try to save to database
        try
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Logs.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception dbError)
        {
            entry.Details = string.Join(" | ", new[]
            {
                entry.Details,
                $"Database log failure: {dbError.Message}"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        // 2. Write to file (with retry and proper file locking)
        var folder = Path.Combine(_environment.ContentRootPath, "logs");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"emilimo-{entry.TimestampUtc:yyyy-MM-dd}.log");
        var line = JsonSerializer.Serialize(entry, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }) + Environment.NewLine;

        await FileLock.WaitAsync(cancellationToken);
        try
        {
            int retries = 5;
            while (retries > 0)
            {
                try
                {
                    // Use FileStream with asynchronous flag and proper sharing
                    await using (var stream = new FileStream(
                        path,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read,   // Allow other readers, but we have a lock for writers
                        bufferSize: 4096,
                        FileOptions.Asynchronous))
                    await using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(line.AsMemory(), cancellationToken);
                        await writer.FlushAsync(cancellationToken);
                    }
                    break; // Success
                }
                catch (IOException) when (retries > 1)
                {
                    retries--;
                    // Wait a bit before retrying
                    await Task.Delay(100 * (5 - retries), cancellationToken);
                }
                catch
                {
                    // Re-throw non-IO exceptions
                    throw;
                }
            }
        }
        finally
        {
            FileLock.Release();
        }
    }
}
