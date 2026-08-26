using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.API.Services;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;
using UCP.API.Platform;

namespace UCP.API.Controllers;

[ApiController, Route("api/database-backups"), Authorize(Roles = "Admin,SuperAdmin")]
public sealed class DatabaseBackupsController(AppDbContext db, IDatabaseBackupService backups, IDistributedLock distributedLock, ISystemLogWriter logWriter) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await db.SystemConfigs.AsNoTracking().Where(x => !x.IsArchived && (x.ConfigKey == "auto_backup_enabled" || x.ConfigKey == "auto_backup_time")).ToListAsync();
        string? Value(string key) => rows.FirstOrDefault(x => x.ConfigKey == key)?.ConfigValue;
        return Ok(new { enabled = bool.TryParse(Value("auto_backup_enabled"), out var enabled) && enabled, time = Value("auto_backup_time") ?? "02:00", files = backups.List() });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(BackupSettingsRequest request)
    {
        if (!TimeOnly.TryParse(request.Time, out var time)) return BadRequest(new { message = "Enter a valid backup time." });
        await Upsert("auto_backup_enabled", request.Enabled.ToString(), "Enable daily automatic database backups");
        await Upsert("auto_backup_time", time.ToString("HH:mm"), "Local time for the daily automatic database backup");
        await db.SaveChangesAsync();
        return Ok(new { message = "Automatic backup settings saved." });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await using var lease = await distributedLock.TryAcquireAsync("emilimo:database-maintenance", cancellationToken);
        if (lease == null) return Conflict(new { message = "Another backup or restore operation is currently running." });
        try { return Ok(new { message = "Database backup created successfully.", file = await backups.CreateAsync("manual", cancellationToken) }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{fileName}/download")]
    public IActionResult Download(string fileName)
    {
        try { var path = backups.ResolveFile(fileName); return PhysicalFile(path, "application/octet-stream", Path.GetFileName(path)); }
        catch (FileNotFoundException) { return NotFound(new { message = "Backup file was not found." }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{fileName}/restore")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Restore(string fileName, RestoreBackupRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Confirmation, "RESTORE", StringComparison.Ordinal))
            return BadRequest(new { message = "Enter RESTORE exactly to confirm the database restore." });
        await using var lease = await distributedLock.TryAcquireAsync("emilimo:database-maintenance", cancellationToken);
        if (lease == null) return Conflict(new { message = "Another backup or restore operation is currently running." });
        try
        {
            var result = await backups.RestoreAsync(fileName, cancellationToken);
            await logWriter.WriteAsync(new SystemLog
            {
                Level = "Warning", Category = "System", Process = "DatabaseMaintenance", Action = "Restore",
                Message = $"Database restored from {result.RestoredFile}. A safety backup was created as {result.SafetyBackupFile}."
            }, cancellationToken);
            return Ok(new { message = "Database restored successfully. Sign in again to load the restored data.", result });
        }
        catch (FileNotFoundException) { return NotFound(new { message = "Backup file was not found." }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task Upsert(string key, string value, string description)
    {
        var row = await db.SystemConfigs.FirstOrDefaultAsync(x => x.ConfigKey == key);
        if (row == null) db.SystemConfigs.Add(new SystemConfig { Id = Guid.NewGuid(), ConfigKey = key, ConfigValue = value, Description = description, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1 });
        else { row.ConfigValue = value; row.Description = description; row.IsArchived = false; row.UpdatedDate = DateTime.UtcNow; row.Version++; }
    }

    public sealed record BackupSettingsRequest(bool Enabled, string Time);
    public sealed record RestoreBackupRequest(string Confirmation);
}
