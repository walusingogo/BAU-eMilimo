using eMilimo.API.Platform;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

/// <summary>
/// Watches configured response folders for result manifests produced by the
/// local Sage Pastel Partner connector. It never opens or modifies a Pastel
/// company database and never treats file delivery alone as ledger posting.
/// </summary>
public sealed class AccountingImportResponseWorker(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IConfiguration configuration,
    ILogger<AccountingImportResponseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ScanAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Accounting import-response scan failed."); }

            var seconds = Math.Clamp(configuration.GetValue("AccountingIntegration:ImportScanSeconds", 60), 30, 3600);
            await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
        }
    }

    internal async Task ScanAsync(CancellationToken cancellationToken)
    {
        await using var lease = await distributedLock.TryAcquireAsync("emilimo:accounting-import-response-worker", cancellationToken);
        if (lease is null) return;

        await using var discoveryScope = scopeFactory.CreateAsyncScope();
        var db = discoveryScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profileIds = await db.AccountingIntegrationProfiles.AsNoTracking()
            .Where(x => x.IsEnabled && x.IntegrationSettingId != null)
            .Select(x => x.Id).ToListAsync(cancellationToken);

        foreach (var profileId in profileIds)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IAccountingIntegrationService>();
                var result = await service.ScanImportResponsesAsync(profileId, null, cancellationToken);
                if (result.Processed > 0 || result.Failed > 0)
                    logger.LogInformation(
                        "Accounting response scan for profile {ProfileId}: {Processed} processed, {Skipped} skipped, {Failed} failed.",
                        profileId, result.Processed, result.Skipped, result.Failed);
            }
            catch (DirectoryNotFoundException ex)
            {
                logger.LogWarning("Accounting response folder for profile {ProfileId} is unavailable: {Message}", profileId, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Accounting response scan for profile {ProfileId} is not configured: {Message}", profileId, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Accounting response scan failed for profile {ProfileId}.", profileId);
            }
        }
    }
}
