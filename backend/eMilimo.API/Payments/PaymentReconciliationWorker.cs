using eMilimo.API.Platform;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class PaymentReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    ILogger<PaymentReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Digital payment reconciliation batch failed."); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    internal async Task RunBatchAsync(CancellationToken cancellationToken)
    {
        await using var workerLease = await distributedLock.TryAcquireAsync("emilimo:digital-payment:reconciliation-worker", cancellationToken);
        if (workerLease is null) return;
        await using var discoveryScope = scopeFactory.CreateAsyncScope();
        var factory = discoveryScope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var discoveryDb = await factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var ids = await discoveryDb.DigitalCollections.AsNoTracking()
            .Where(x => x.PaymentProviderConfigurationId != null && x.ProviderTransactionId != null &&
                        x.ReconciliationStatus == PaymentReconciliationStatuses.Scheduled &&
                        (x.NextReconciliationAtUtc == null || x.NextReconciliationAtUtc <= now) &&
                        (x.Status == PaymentTransactionStatuses.Pending || x.Status == PaymentTransactionStatuses.Processing ||
                         x.Status == PaymentTransactionStatuses.Unknown))
            .OrderBy(x => x.NextReconciliationAtUtc)
            .Select(x => x.Id)
            .Take(25)
            .ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            try { await ProcessAsync(id, cancellationToken); }
            catch (Exception ex) { logger.LogError(ex, "Digital payment {PaymentId} recovery attempt failed.", id); }
        }
    }

    private async Task ProcessAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var providers = scope.ServiceProvider.GetRequiredService<IPaymentProviderRegistry>();
        var configurations = scope.ServiceProvider.GetRequiredService<IPaymentProviderConfigurationStore>();
        var finalization = scope.ServiceProvider.GetRequiredService<IPaymentFinalizationService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IPaymentWebhookProcessor>();
        var payment = await db.DigitalCollections
            .Include(x => x.PaymentProviderConfiguration)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment?.PaymentProviderConfiguration is null || string.IsNullOrWhiteSpace(payment.ProviderTransactionId)) return;

        if (!payment.PaymentProviderConfiguration.IsEnabled)
        {
            payment.ReconciliationStatus = PaymentReconciliationStatuses.ManualReview;
            payment.NextReconciliationAtUtc = null;
            payment.FailureReason = "Automated verification paused because the payment provider configuration is disabled. Enable and validate the provider before retrying verification.";
            payment.UpdatedDate = DateTime.UtcNow;
            payment.Version++;
            db.DigitalCollectionEvents.Add(Event(payment, "RECONCILIATION_PROVIDER_DISABLED", payment.Status, payment.Status));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Digital payment {PaymentId} moved to manual review because provider configuration {ConfigurationId} is disabled.",
                payment.Id, payment.PaymentProviderConfigurationId);
            return;
        }

        var settings = await configurations.GetRuntimeSettingsAsync(payment.PaymentProviderConfiguration.Id, true, cancellationToken);
        var maximumAttempts = Setting(settings, "PollingMaximumAttempts", 12);
        var attempt = payment.ReconciliationAttemptCount + 1;
        if (attempt > maximumAttempts)
        {
            payment.ReconciliationStatus = PaymentReconciliationStatuses.ManualReview;
            payment.NextReconciliationAtUtc = null;
            payment.FailureReason = "Automated payment verification reached its configured attempt limit.";
            payment.UpdatedDate = DateTime.UtcNow;
            payment.Version++;
            db.DigitalCollectionEvents.Add(Event(payment, "RECONCILIATION_MANUAL_REVIEW", payment.Status, payment.Status));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        payment.ReconciliationStatus = PaymentReconciliationStatuses.Polling;
        payment.ReconciliationAttemptCount = attempt;
        payment.LastReconciliationAtUtc = DateTime.UtcNow;
        payment.NextReconciliationAtUtc = DateTime.UtcNow.AddSeconds(Backoff(settings, attempt));
        payment.UpdatedDate = DateTime.UtcNow;
        payment.Version++;
        db.DigitalCollectionEvents.Add(Event(payment, "RECONCILIATION_ATTEMPT_STARTED", payment.Status, payment.Status,
            new { AttemptNumber = attempt, Reason = "Scheduled provider reconciliation" }));
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var provider = providers.GetRequired(payment.PaymentProviderConfiguration.ProviderKey);
            var status = await provider.QueryStatusAsync(payment.ProviderTransactionId, settings, cancellationToken);
            var finalized = await finalization.ApplyVerifiedStatusAsync(payment.Id, new ProviderVerifiedPayment(
                payment.ProviderTransactionId, status.Status, status.VerifiedAmount, status.VerifiedCurrencyCode,
                status.ProviderFee, status.NetSettlementAmount, status.ProviderCompletedAtUtc, status.ProviderMessage),
                "AUTOMATED_STATUS_QUERY", $"reconcile-{Guid.NewGuid():N}", cancellationToken: cancellationToken);
            if (finalized.Completed && !finalized.AlreadyCompleted)
                await notifications.SendReceiptNotificationAsync(finalized.PaymentReference, cancellationToken);
            if (!finalized.Completed && finalized.Status is PaymentTransactionStatuses.Pending or PaymentTransactionStatuses.Processing or PaymentTransactionStatuses.Unknown)
            {
                db.ChangeTracker.Clear();
                var pending = await db.DigitalCollections.SingleAsync(x => x.Id == id, cancellationToken);
                pending.ReconciliationStatus = PaymentReconciliationStatuses.Scheduled;
                pending.NextReconciliationAtUtc = DateTime.UtcNow.AddSeconds(Backoff(settings, attempt));
                pending.UpdatedDate = DateTime.UtcNow;
                pending.Version++;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            var uncertain = await db.DigitalCollections.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (uncertain is null || uncertain.Status == PaymentTransactionStatuses.Succeeded) return;
            uncertain.ReconciliationStatus = attempt >= maximumAttempts
                ? PaymentReconciliationStatuses.ManualReview
                : PaymentReconciliationStatuses.Scheduled;
            uncertain.NextReconciliationAtUtc = attempt >= maximumAttempts ? null : DateTime.UtcNow.AddSeconds(Backoff(settings, attempt));
            uncertain.FailureReason = Truncate($"Reconciliation attempt {attempt} failed: {ex.Message}", 1000);
            uncertain.UpdatedDate = DateTime.UtcNow;
            uncertain.Version++;
            db.DigitalCollectionEvents.Add(Event(uncertain, "RECONCILIATION_ATTEMPT_FAILED", uncertain.Status, uncertain.Status));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static int Backoff(IReadOnlyDictionary<string, string> settings, int attempt)
    {
        var initial = Setting(settings, "PollingInitialSeconds", 20);
        var maximum = Setting(settings, "PollingMaximumSeconds", 300);
        return (int)Math.Min(maximum, initial * Math.Pow(2, Math.Min(attempt - 1, 10)));
    }

    private static int Setting(IReadOnlyDictionary<string, string> settings, string key, int fallback) =>
        settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static DigitalCollectionEvent Event(
        DigitalCollection payment,
        string type,
        string previous,
        string next,
        object? details = null) => new()
    {
        Id = Guid.NewGuid(), DigitalCollectionId = payment.Id, EventType = type, PreviousStatus = previous,
        NewStatus = next, Source = "RECONCILIATION_WORKER", CorrelationId = $"reconcile-{Guid.NewGuid():N}",
        SanitizedDetailsJson = details is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(details),
        OccurredAtUtc = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
    };

    private static string Truncate(string value, int maximum) => value.Length <= maximum ? value : value[..maximum];
}
