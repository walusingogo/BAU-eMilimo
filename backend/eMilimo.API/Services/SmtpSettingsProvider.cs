using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public interface ISmtpSettingsProvider
{
    Task<SmtpSetting> GetAsync(CancellationToken cancellationToken = default);
    void InvalidateCache();
}

/// <summary>
/// Reads SMTP configuration from the database (System Settings screen is the
/// source of truth), with a short in-memory cache so a busy notification
/// burst doesn't hit the database on every single send. Falls back to a
/// disabled, empty settings object if no row exists yet (the migration seeds
/// one, so this is a defensive fallback rather than the expected path).
/// </summary>
public class SmtpSettingsProvider : ISmtpSettingsProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static SmtpSetting? _cached;
    private static DateTime _cachedAtUtc;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public SmtpSettingsProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void InvalidateCache()
    {
        _cached = null;
    }

    public async Task<SmtpSetting> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached != null && DateTime.UtcNow - _cachedAtUtc < CacheDuration)
            return _cached;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cached != null && DateTime.UtcNow - _cachedAtUtc < CacheDuration)
                return _cached;

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var settings = await context.SmtpSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            settings ??= new SmtpSetting { Enabled = false };

            _cached = settings;
            _cachedAtUtc = DateTime.UtcNow;
            return settings;
        }
        finally
        {
            _lock.Release();
        }
    }
}