using System.Data;
using eMilimo.API.Platform;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed record ProviderVerifiedPayment(
    string ProviderTransactionId,
    string Status,
    decimal? Amount,
    string? CurrencyCode,
    decimal? ProviderFee,
    decimal? NetSettlementAmount,
    DateTime? ProviderCompletedAtUtc,
    string? ProviderMessage);

public sealed record PaymentFinalizationResult(bool Completed, bool AlreadyCompleted, string PaymentReference,
    string Status, string ReconciliationStatus, string Message);

public interface IPaymentFinalizationService
{
    Task<PaymentFinalizationResult> ApplyVerifiedStatusAsync(Guid paymentTransactionId, ProviderVerifiedPayment verified,
        string source, string correlationId, Guid? actorUserId = null, CancellationToken cancellationToken = default);
}

public sealed class PaymentFinalizationService(
    AppDbContext db,
    IReferenceNumberService referenceNumbers,
    IContributionPostingService posting,
    IDistributedLock distributedLock,
    ILogger<PaymentFinalizationService> logger) : IPaymentFinalizationService
{
    public async Task<PaymentFinalizationResult> ApplyVerifiedStatusAsync(Guid paymentTransactionId,
        ProviderVerifiedPayment verified, string source, string correlationId, Guid? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        await using var lease = await distributedLock.TryAcquireAsync($"emilimo:digital-payment:finalize:{paymentTransactionId:N}", cancellationToken);
        if (lease is null) throw new PaymentConflictException("This payment is already being finalized. Retry shortly.");

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var payment = await db.DigitalCollections
                .Include(x => x.PaymentMethodDefinition)
                .Include(x => x.Lines).ThenInclude(x => x.ContributionType)
                .SingleOrDefaultAsync(x => x.Id == paymentTransactionId, cancellationToken)
                ?? throw new KeyNotFoundException("Digital payment transaction was not found.");
            if (!string.Equals(payment.ProviderTransactionId, verified.ProviderTransactionId, StringComparison.OrdinalIgnoreCase))
                throw new PaymentConflictException("The verified provider reference does not belong to this payment.");

            var incoming = verified.Status.Trim().ToUpperInvariant();
            if (incoming == PaymentTransactionStatuses.Succeeded)
            {
                ValidateSuccess(payment, verified);
                if (payment.Status == PaymentTransactionStatuses.Succeeded)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return Result(payment, true, "Payment was already finalized.");
                }
                if (payment.Status is PaymentTransactionStatuses.Cancelled or PaymentTransactionStatuses.Reversed)
                    throw new PaymentConflictException($"A {payment.Status} payment cannot be finalized automatically.");
                if (payment.Status is PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Expired)
                    throw new PaymentConflictException($"MTN reports success for a payment recorded as {payment.Status}. Manual reconciliation is required.");
                await PostContributionsAsync(payment, verified, source, correlationId, actorUserId, cancellationToken);
                if (payment.ProviderEnvironmentSnapshot.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase))
                    db.DigitalCollectionEvents.Add(NewEvent(payment.Id, "SANDBOX_FULL_WORKFLOW_POSTED", PaymentTransactionStatuses.Succeeded,
                        PaymentTransactionStatuses.Succeeded, source, correlationId, actorUserId));
                payment.ReconciliationStatus = PaymentReconciliationStatuses.Recovered;
                payment.NextReconciliationAtUtc = null;
                payment.FailureReason = null;
            }
            else ApplyNonSuccess(payment, incoming, verified.ProviderMessage, source, correlationId, actorUserId);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result(payment, false, payment.Status == PaymentTransactionStatuses.Succeeded
                ? payment.ProviderEnvironmentSnapshot.Equals("SANDBOX", StringComparison.OrdinalIgnoreCase)
                    ? "Sandbox payment verified and the complete contribution and ledger workflow was posted."
                    : "Payment verified and contributions posted."
                : "Verified provider status recorded.");
        }
        catch (PaymentConflictException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Verified payment {PaymentTransactionId} could not be finalized.", paymentTransactionId);
            db.ChangeTracker.Clear();
            var payment = await db.DigitalCollections.SingleOrDefaultAsync(x => x.Id == paymentTransactionId, cancellationToken);
            if (payment is not null && payment.Status != PaymentTransactionStatuses.Succeeded)
            {
                payment.ReconciliationStatus = PaymentReconciliationStatuses.ManualReview;
                payment.NextReconciliationAtUtc = null;
                payment.FailureReason = Truncate("Provider verification succeeded but financial posting requires recovery: " + ex.Message, 1000);
                payment.UpdatedDate = DateTime.UtcNow;
                payment.Version++;
                db.DigitalCollectionEvents.Add(NewEvent(payment.Id, "FINALIZATION_FAILED", payment.Status, payment.Status, source, correlationId, actorUserId));
                await db.SaveChangesAsync(cancellationToken);
            }
            throw;
        }
    }

    private async Task PostContributionsAsync(DigitalCollection payment, ProviderVerifiedPayment verified,
        string source, string correlationId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var completedAt = verified.ProviderCompletedAtUtc ?? DateTime.UtcNow;
        var previous = payment.Status;
        foreach (var line in payment.Lines.OrderBy(x => x.CreatedDate))
        {
            if (line.ContributionId.HasValue)
                throw new InvalidOperationException("A payment line is already linked to a contribution while the payment remains incomplete.");
            var contribution = new Contribution
            {
                Id = Guid.NewGuid(),
                ContributionNumber = await referenceNumbers.NextAsync("CONTRIBUTION", cancellationToken: cancellationToken),
                ContributionTypeId = line.ContributionTypeId, SourceId = payment.ContributionSourceId, Amount = line.Amount,
                ContributionDate = completedAt, Description = $"Digital Giving payment {payment.PaymentReference}",
                ReferenceNumber = payment.ProviderTransactionId,
                PaymentMethod = payment.PaymentMethodDefinition?.Code ?? "DIGITAL",
                PaymentMethodDefinitionId = payment.PaymentMethodDefinitionId,
                Status = ContributionStatus.CONFIRMED.ToString(), OriginType = "DIGITAL_GIVING", MemberId = payment.MemberId,
                ContributorNameSnapshot = payment.GiverType == DigitalGiverTypes.Anonymous ? null : payment.ContributorNameSnapshot,
                CurrencyCode = payment.CurrencyCode, OriginalCurrencyAmount = line.Amount, ExchangeRateSnapshot = 1m,
                CreatedById = actorUserId, UpdatedById = actorUserId, CreatedDate = completedAt, UpdatedDate = completedAt, Version = 1
            };
            contribution.Allocations.Add(new ContributionAllocation
            {
                Id = Guid.NewGuid(), ContributionId = contribution.Id, ContributionTypeId = line.ContributionTypeId,
                GroupId = line.GroupId, SectionId = line.SectionId, CommitteeId = line.CommitteeId,
                AllocatedAmount = line.Amount, PercentageAllocation = 100m, IsPrimary = true,
                Notes = $"Digital Giving payment {payment.PaymentReference}", CreatedById = actorUserId, UpdatedById = actorUserId,
                CreatedDate = completedAt, UpdatedDate = completedAt, Version = 1
            });
            db.Contributions.Add(contribution);
            await db.SaveChangesAsync(cancellationToken);
            await posting.PostAsync(new ContributionPostingRequest(contribution.Id, actorUserId, completedAt, "VERIFIED_DIGITAL_PAYMENT"), cancellationToken);
            line.ContributionId = contribution.Id;
            line.UpdatedById = actorUserId;
            line.UpdatedDate = DateTime.UtcNow;
            line.Version++;
        }

        payment.ProviderFee = verified.ProviderFee ?? 0m;
        payment.NetSettlementAmount = verified.NetSettlementAmount ?? payment.GrossAmount - payment.ProviderFee;
        payment.ProviderCompletedAtUtc = completedAt;
        payment.Status = PaymentTransactionStatuses.Succeeded;
        payment.SettlementStatus = PaymentSettlementStatuses.Settled;
        payment.UpdatedById = actorUserId;
        payment.UpdatedDate = DateTime.UtcNow;
        payment.Version++;
        db.DigitalCollectionEvents.Add(NewEvent(payment.Id, "PAYMENT_VERIFIED_AND_POSTED", previous,
            PaymentTransactionStatuses.Succeeded, source, correlationId, actorUserId));
    }

    private static void ValidateSuccess(DigitalCollection payment, ProviderVerifiedPayment verified)
    {
        if (!verified.Amount.HasValue || verified.Amount.Value != payment.GrossAmount)
            throw new PaymentConflictException("Provider amount does not match the payment intent.");
        if (string.IsNullOrWhiteSpace(verified.CurrencyCode) ||
            !string.Equals(verified.CurrencyCode, payment.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            throw new PaymentConflictException("Provider currency does not match the payment intent.");
        var fee = verified.ProviderFee ?? 0m;
        var net = verified.NetSettlementAmount ?? payment.GrossAmount - fee;
        if (fee < 0 || fee > payment.GrossAmount || net < 0 || net != payment.GrossAmount - fee)
            throw new PaymentConflictException("Provider fee or net settlement amount is inconsistent.");
        if (payment.Lines.Count == 0 || payment.Lines.Sum(x => x.Amount) != payment.GrossAmount)
            throw new PaymentConflictException("Payment contribution lines do not equal the verified amount.");
    }

    private static void ApplyNonSuccess(DigitalCollection payment, string incoming, string? message,
        string source, string correlationId, Guid? actorUserId)
    {
        if (payment.Status == PaymentTransactionStatuses.Succeeded)
            throw new PaymentConflictException("A posted payment cannot be downgraded.");
        if (incoming is not (PaymentTransactionStatuses.Pending or PaymentTransactionStatuses.Processing or
            PaymentTransactionStatuses.Unknown or PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Cancelled or PaymentTransactionStatuses.Expired))
            throw new PaymentConflictException($"Unsupported verified provider status '{incoming}'.");
        var previous = payment.Status;
        payment.Status = incoming;
        payment.ReconciliationStatus = incoming is PaymentTransactionStatuses.Pending or PaymentTransactionStatuses.Processing or PaymentTransactionStatuses.Unknown
            ? PaymentReconciliationStatuses.Scheduled : PaymentReconciliationStatuses.None;
        if (payment.ReconciliationStatus == PaymentReconciliationStatuses.None) payment.NextReconciliationAtUtc = null;
        payment.FailureReason = incoming == PaymentTransactionStatuses.Failed ? Truncate(message, 1000) : null;
        payment.UpdatedDate = DateTime.UtcNow;
        payment.UpdatedById = actorUserId;
        payment.Version++;
        payment.Events.Add(NewEvent(payment.Id, "PROVIDER_STATUS_VERIFIED", previous, incoming, source, correlationId, actorUserId));
    }

    private static PaymentFinalizationResult Result(DigitalCollection payment, bool already, string message) =>
        new(payment.Status == PaymentTransactionStatuses.Succeeded, already, payment.PaymentReference,
            payment.Status, payment.ReconciliationStatus, message);

    private static DigitalCollectionEvent NewEvent(Guid paymentId, string type, string? previous, string next,
        string source, string correlationId, Guid? actorUserId) => new()
        {
            Id = Guid.NewGuid(), DigitalCollectionId = paymentId, EventType = type,
            PreviousStatus = previous, NewStatus = next, Source = source, CorrelationId = correlationId,
            OccurredAtUtc = DateTime.UtcNow, CreatedById = actorUserId, UpdatedById = actorUserId,
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
        };

    private static string? Truncate(string? value, int maximum) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= maximum ? value : value[..maximum];
}
