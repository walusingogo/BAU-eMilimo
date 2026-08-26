using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;
using UCP.API.Platform;

namespace UCP.API.Services;

public sealed record DatabaseBackupFile(string FileName, long SizeBytes, DateTime CreatedAtUtc);

public interface IDatabaseBackupService
{
    Task<DatabaseBackupFile> CreateAsync(string source, CancellationToken cancellationToken = default);
    Task<DatabaseRestoreResult> RestoreAsync(string fileName, CancellationToken cancellationToken = default);
    IReadOnlyList<DatabaseBackupFile> List();
    string ResolveFile(string fileName);
}

public sealed record DatabaseRestoreResult(string RestoredFile, string SafetyBackupFile, DateTime CompletedAtUtc);

public sealed class DatabaseBackupService(IConfiguration configuration, IFileStorage storage, ILogger<DatabaseBackupService> logger) : IDatabaseBackupService
{
    public async Task<DatabaseBackupFile> CreateAsync(string source, CancellationToken cancellationToken = default)
    {
        var provider = configuration["Database:Provider"] ?? "PostgreSQL";
        if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Automated backups currently support PostgreSQL databases only.");

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The database connection string is not configured.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var safeSource = source.ToLowerInvariant() switch { "automatic" => "automatic", "pre_restore" => "pre_restore", _ => "manual" };
        var fileName = $"emilimo_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeSource}.backup";
        var storageKey = $"backups/{fileName}";
        var path = storage.GetPhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var executable = ResolvePgDumpExecutable(configuration["Backup:PostgreSqlDumpPath"]);
        var host = connection.Host ?? throw new InvalidOperationException("The PostgreSQL host is not configured.");
        var username = connection.Username ?? throw new InvalidOperationException("The PostgreSQL username is not configured.");
        var database = connection.Database ?? throw new InvalidOperationException("The PostgreSQL database name is not configured.");

        var start = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("--host"); start.ArgumentList.Add(host);
        start.ArgumentList.Add("--port"); start.ArgumentList.Add(connection.Port.ToString());
        start.ArgumentList.Add("--username"); start.ArgumentList.Add(username);
        start.ArgumentList.Add("--dbname"); start.ArgumentList.Add(database);
        start.ArgumentList.Add("--format=custom");
        start.ArgumentList.Add("--no-password");
        start.ArgumentList.Add("--file"); start.ArgumentList.Add(path);
        if (!string.IsNullOrEmpty(connection.Password)) start.Environment["PGPASSWORD"] = connection.Password;

        try
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException("The PostgreSQL backup process could not be started.");
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = (await errorTask).Trim();
            if (process.ExitCode != 0)
            {
                if (File.Exists(path)) File.Delete(path);
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"pg_dump exited with code {process.ExitCode}." : error);
            }
            var file = new FileInfo(path);
            logger.LogInformation("Database {Source} backup created: {FileName} ({Size} bytes).", safeSource, file.Name, file.Length);
            return new DatabaseBackupFile(file.Name, file.Length, file.CreationTimeUtc);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException("PostgreSQL pg_dump was not found. Configure Backup:PostgreSqlDumpPath with the full pg_dump executable path.", ex);
        }
    }

    public IReadOnlyList<DatabaseBackupFile> List()
    {
        return storage.List("backups")
            .Where(x => x.Key.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
            .Select(x => new DatabaseBackupFile(Path.GetFileName(x.Key), x.Size, x.LastModifiedUtc)).ToArray();
    }

    public async Task<DatabaseRestoreResult> RestoreAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var provider = configuration["Database:Provider"] ?? "PostgreSQL";
        if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Database restore currently supports PostgreSQL databases only.");

        var backupPath = ResolveFile(fileName);
        var safetyBackup = await CreateAsync("pre_restore", cancellationToken);
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("The database connection string is not configured.");
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var host = connection.Host ?? throw new InvalidOperationException("The PostgreSQL host is not configured.");
        var username = connection.Username ?? throw new InvalidOperationException("The PostgreSQL username is not configured.");
        var database = connection.Database ?? throw new InvalidOperationException("The PostgreSQL database name is not configured.");
        var executable = ResolvePostgreSqlExecutable(configuration["Backup:PostgreSqlRestorePath"], "pg_restore");

        var start = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("--host"); start.ArgumentList.Add(host);
        start.ArgumentList.Add("--port"); start.ArgumentList.Add(connection.Port.ToString());
        start.ArgumentList.Add("--username"); start.ArgumentList.Add(username);
        start.ArgumentList.Add("--dbname"); start.ArgumentList.Add(database);
        start.ArgumentList.Add("--clean");
        start.ArgumentList.Add("--if-exists");
        start.ArgumentList.Add("--no-owner");
        start.ArgumentList.Add("--no-privileges");
        start.ArgumentList.Add("--single-transaction");
        start.ArgumentList.Add("--exit-on-error");
        start.ArgumentList.Add(backupPath);
        if (!string.IsNullOrEmpty(connection.Password)) start.Environment["PGPASSWORD"] = connection.Password;

        try
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException("The PostgreSQL restore process could not be started.");
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var error = (await errorTask).Trim();
            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? $"pg_restore exited with code {process.ExitCode}. The restore transaction was rolled back."
                    : $"Database restore failed and was rolled back. {error}");
            logger.LogWarning("Database restored from {FileName}. Safety backup: {SafetyBackup}.", fileName, safetyBackup.FileName);
            return new DatabaseRestoreResult(fileName, safetyBackup.FileName, DateTime.UtcNow);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException("PostgreSQL pg_restore was not found. Configure Backup:PostgreSqlRestorePath with its full executable path.", ex);
        }
    }

    public string ResolveFile(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (!safeName.Equals(fileName, StringComparison.Ordinal) || !safeName.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid backup file name.");
        var key = $"backups/{safeName}";
        if (!storage.Exists(key)) throw new FileNotFoundException();
        return storage.GetPhysicalPath(key);
    }

    private static string ResolvePgDumpExecutable(string? configured)
    {
        return ResolvePostgreSqlExecutable(configured, "pg_dump");
    }

    private static string ResolvePostgreSqlExecutable(string? configured, string command)
    {
        if (!string.IsNullOrWhiteSpace(configured) && !configured.Equals(command, StringComparison.OrdinalIgnoreCase)) return configured;
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var root = Path.Combine(programFiles, "PostgreSQL");
            if (Directory.Exists(root))
            {
                var discovered = Directory.GetDirectories(root)
                    .Select(x => Path.Combine(x, "bin", $"{command}.exe"))
                    .Where(File.Exists)
                    .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (discovered != null) return discovered;
            }
        }
        return configured ?? command;
    }
}

public sealed class DatabaseBackupScheduler(IServiceScopeFactory scopes, IDistributedLock distributedLock, ILogger<DatabaseBackupScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var configs = await db.SystemConfigs.Where(x => !x.IsArchived && (x.ConfigKey == "auto_backup_enabled" || x.ConfigKey == "auto_backup_time" || x.ConfigKey == "auto_backup_last_run_date")).ToListAsync(stoppingToken);
                string? Value(string key) => configs.FirstOrDefault(x => x.ConfigKey == key)?.ConfigValue;
                if (!bool.TryParse(Value("auto_backup_enabled"), out var enabled) || !enabled) continue;
                if (!TimeOnly.TryParse(Value("auto_backup_time"), out var scheduled)) scheduled = new TimeOnly(2, 0);
                var localNow = DateTime.Now;
                if (TimeOnly.FromDateTime(localNow) < scheduled || Value("auto_backup_last_run_date") == localNow.ToString("yyyy-MM-dd")) continue;

        await using var lease = await distributedLock.TryAcquireAsync("emilimo:database-maintenance", stoppingToken);
                if (lease == null) continue;
                await scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>().CreateAsync("automatic", stoppingToken);
                var lastRun = configs.FirstOrDefault(x => x.ConfigKey == "auto_backup_last_run_date");
                if (lastRun == null)
                    db.SystemConfigs.Add(new SystemConfig { Id = Guid.NewGuid(), ConfigKey = "auto_backup_last_run_date", ConfigValue = localNow.ToString("yyyy-MM-dd"), Description = "Last successful automatic database backup date", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1 });
                else { lastRun.ConfigValue = localNow.ToString("yyyy-MM-dd"); lastRun.UpdatedDate = DateTime.UtcNow; lastRun.Version++; }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Scheduled database backup failed."); }
        }
    }
}
