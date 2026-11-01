using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class PaymentDiagnosticsRetentionWorker(
    IDbContextFactory<AppDbContext> dbFactory,
    IConfiguration configuration,
    ILogger<PaymentDiagnosticsRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var days = Math.Clamp(configuration.GetValue("Payments:DiagnosticsRetentionDays", 90), 7, 730);
                await using var db = await dbFactory.CreateDbContextAsync(stoppingToken);
                var deleted = await db.PaymentApiExchanges
                    .Where(x => x.RequestedAtUtc < DateTime.UtcNow.AddDays(-days))
                    .ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0) logger.LogInformation("Removed {Count} expired sanitized payment API diagnostics.", deleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Payment diagnostics retention cleanup failed."); }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
