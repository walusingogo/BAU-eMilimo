using eMilimo.API.Platform;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public interface IPublicPaymentStatusRefreshService
{
    Task RefreshAsync(string paymentReference, string publicStatusToken, string correlationId,
        CancellationToken cancellationToken = default);
}

public sealed class PublicPaymentStatusRefreshService(
    AppDbContext db,
    IPaymentProviderRegistry providers,
    IPaymentProviderConfigurationStore configurations,
    IPaymentFinalizationService finalization,
    IPaymentWebhookProcessor notifications,
    IDistributedLock distributedLock,
    ILogger<PublicPaymentStatusRefreshService> logger) : IPublicPaymentStatusRefreshService
{
    private static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromSeconds(3);

    public async Task RefreshAsync(string paymentReference, string publicStatusToken, string correlationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedReference = paymentReference.Trim().ToUpperInvariant();
        var payment = await db.DigitalCollections
            .Include(x => x.PaymentProviderConfiguration)
            .SingleOrDefaultAsync(x => x.PaymentReference == normalizedReference, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");
        if (!DigitalGivingSecurity.FixedTimeEquals(payment.PublicStatusTokenHash, publicStatusToken))
            throw new KeyNotFoundException("Payment not found.");
        if (IsTerminal(payment.Status) || payment.PaymentProviderConfiguration is null ||
            string.IsNullOrWhiteSpace(payment.ProviderTransactionId)) return;
        if (!payment.PaymentProviderConfiguration.IsEnabled) return;
        if (payment.LastReconciliationAtUtc.HasValue &&
            payment.LastReconciliationAtUtc.Value > DateTime.UtcNow.Subtract(MinimumRefreshInterval)) return;

        await using var refreshLock = await distributedLock.TryAcquireAsync(
            $"emilimo:digital-payment:public-refresh:{payment.Id:N}", cancellationToken);
        if (refreshLock is null) return;

        try
        {
            payment.LastReconciliationAtUtc = DateTime.UtcNow;
            payment.UpdatedDate = DateTime.UtcNow;
            payment.Version++;
            await db.SaveChangesAsync(cancellationToken);

            var settings = await configurations.GetRuntimeSettingsAsync(
                payment.PaymentProviderConfiguration.Id, true, cancellationToken);
            var provider = providers.GetRequired(payment.PaymentProviderConfiguration.ProviderKey);
            var result = await provider.QueryStatusAsync(payment.ProviderTransactionId, settings, cancellationToken);
            var finalized = await finalization.ApplyVerifiedStatusAsync(payment.Id, new ProviderVerifiedPayment(
                payment.ProviderTransactionId,
                result.Status,
                result.VerifiedAmount,
                result.VerifiedCurrencyCode,
                result.ProviderFee,
                result.NetSettlementAmount,
                result.ProviderCompletedAtUtc,
                result.ProviderMessage),
                "PUBLIC_AUTOMATIC_STATUS_REFRESH",
                correlationId,
                cancellationToken: cancellationToken);
            if (finalized.Completed && !finalized.AlreadyCompleted)
                await notifications.SendReceiptNotificationAsync(finalized.PaymentReference, cancellationToken);
        }
        catch (PaymentConflictException)
        {
            // Another callback, worker or browser may have finalized the same payment.
            // The subsequent status read will return the authoritative stored state.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Automatic public status refresh failed for payment {PaymentReference}; the stored status will be returned.",
                normalizedReference);
        }
    }

    private static bool IsTerminal(string status) => status is
        PaymentTransactionStatuses.Succeeded or
        PaymentTransactionStatuses.Failed or
        PaymentTransactionStatuses.Cancelled or
        PaymentTransactionStatuses.Expired or
        PaymentTransactionStatuses.Reversed;
}
