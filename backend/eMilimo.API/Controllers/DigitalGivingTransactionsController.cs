using eMilimo.API.Authorization;
using eMilimo.API.Payments;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;

namespace eMilimo.API.Controllers;

[ApiController]
[Route("api/digital-giving/transactions")]
[Authorize]
public sealed class DigitalGivingTransactionsController(
    AppDbContext db,
    ICurrentUserService currentUser,
    IPaymentWebhookProcessor paymentProcessor,
    IPaymentProviderRegistry providers,
    IPaymentProviderConfigurationStore providerConfigurations,
    IPaymentFinalizationService finalization,
    ILogger<DigitalGivingTransactionsController> logger,
    IDataProtectionProvider dataProtection) : ControllerBase
{
    private readonly IDataProtector _eventProtector = dataProtection.CreateProtector("eMilimo.DigitalGiving.AdminEvent.v1");
    [HttpGet("summary")]
    [RequirePermission("contributions.view")]
    public async Task<IActionResult> Summary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = ApplyPeriod(db.DigitalCollections.AsNoTracking(), from, to);
        var rows = await query.Select(x => new
        {
            x.Status,
            x.SettlementStatus,
            x.GrossAmount,
            PaymentMethod = x.PaymentMethodDefinition != null ? x.PaymentMethodDefinition.Name : "Unknown"
        }).ToListAsync(cancellationToken);

        var successful = rows.Where(x => x.Status == PaymentTransactionStatuses.Succeeded).ToList();
        var flagged = await ApplyPeriod(db.DigitalCollectionEvents.AsNoTracking()
                .Where(x => x.EventType == "INVESTIGATION_FLAGGED")
                .Select(x => x.DigitalCollection!), from, to)
            .Select(x => x.Id).Distinct().CountAsync(cancellationToken);

        return Ok(new
        {
            transactionCount = rows.Count,
            totalReceived = successful.Sum(x => x.GrossAmount),
            successful = successful.Count,
            pending = rows.Count(x => x.Status is PaymentTransactionStatuses.Created or PaymentTransactionStatuses.Pending or PaymentTransactionStatuses.Processing or PaymentTransactionStatuses.Unknown),
            failed = rows.Count(x => x.Status is PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Cancelled or PaymentTransactionStatuses.Expired),
            reversed = rows.Count(x => x.Status == PaymentTransactionStatuses.Reversed),
            unreconciled = successful.Count(x => x.SettlementStatus != PaymentSettlementStatuses.Reconciled),
            flagged,
            paymentMethods = rows.GroupBy(x => x.PaymentMethod)
                .Select(x => new { name = x.Key, count = x.Count(), amount = x.Where(y => y.Status == PaymentTransactionStatuses.Succeeded).Sum(y => y.GrossAmount) })
                .OrderByDescending(x => x.amount)
        });
    }

    [HttpGet]
    [RequirePermission("contributions.view")]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? settlementStatus,
        [FromQuery] Guid? paymentMethodId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var query = ApplyPeriod(db.DigitalCollections.AsNoTracking(), from, to);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.PaymentReference.ToLower().Contains(term)
                || (x.ProviderTransactionId != null && x.ProviderTransactionId.ToLower().Contains(term))
                || (x.ContributorNameSnapshot != null && x.ContributorNameSnapshot.ToLower().Contains(term))
                || (x.Member != null && ((x.Member.FirstName + " " + x.Member.LastName).ToLower().Contains(term)
                    || x.Member.MembershipNumber.ToLower().Contains(term))));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(settlementStatus)) query = query.Where(x => x.SettlementStatus == settlementStatus.Trim().ToUpper());
        if (paymentMethodId.HasValue) query = query.Where(x => x.PaymentMethodDefinitionId == paymentMethodId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.PaymentReference,
                x.ProviderTransactionId,
                x.GiverType,
                contributorName = x.GiverType == DigitalGiverTypes.Anonymous ? "Anonymous" :
                    (x.Member != null ? x.Member.FirstName + " " + x.Member.LastName : x.ContributorNameSnapshot),
                x.Status,
                x.SettlementStatus,
                x.CurrencyCode,
                x.GrossAmount,
                x.ProviderFee,
                x.NetSettlementAmount,
                paymentMethod = x.PaymentMethodDefinition != null ? x.PaymentMethodDefinition.Name : "Unknown",
                provider = x.PaymentProviderConfiguration != null ? x.PaymentProviderConfiguration.DisplayName : null,
                x.CreatedDate,
                x.ProviderCompletedAtUtc,
                isFlagged = x.Events.Any(e => e.EventType == "INVESTIGATION_FLAGGED")
            }).ToListAsync(cancellationToken);

        return Ok(new { items, total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("export")]
    [RequirePermission("contributions.view")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? settlementStatus,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyPeriod(db.DigitalCollections.AsNoTracking(), from, to);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.PaymentReference.ToLower().Contains(term)
                || (x.ProviderTransactionId != null && x.ProviderTransactionId.ToLower().Contains(term))
                || (x.ContributorNameSnapshot != null && x.ContributorNameSnapshot.ToLower().Contains(term))
                || (x.Member != null && ((x.Member.FirstName + " " + x.Member.LastName).ToLower().Contains(term)
                    || x.Member.MembershipNumber.ToLower().Contains(term))));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(settlementStatus)) query = query.Where(x => x.SettlementStatus == settlementStatus.Trim().ToUpper());
        var totalTransactions = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedDate).Take(10_000)
            .SelectMany(x => x.Lines.Select(line => new
            {
                x.PaymentReference,
                x.ProviderTransactionId,
                x.GiverType,
                contributorName = x.GiverType == DigitalGiverTypes.Anonymous ? "Anonymous" :
                    (x.Member != null ? x.Member.FirstName + " " + x.Member.LastName : x.ContributorNameSnapshot),
                x.Status,
                x.SettlementStatus,
                x.CurrencyCode,
                transactionAmount = x.GrossAmount,
                x.ProviderFee,
                x.NetSettlementAmount,
                paymentMethod = x.PaymentMethodDefinition != null ? x.PaymentMethodDefinition.Name : "Unknown",
                provider = x.PaymentProviderConfiguration != null ? x.PaymentProviderConfiguration.DisplayName : null,
                contributionType = line.ContributionType != null ? line.ContributionType.Name : "Unknown",
                destination = line.Section != null ? line.Section.SectionName : line.Group != null ? line.Group.GroupName : line.Committee != null ? line.Committee.CommitteeName : "Organization-wide",
                lineAmount = line.Amount,
                contributionNumber = line.Contribution != null ? line.Contribution.ContributionNumber : null,
                x.CreatedDate,
                x.ProviderCompletedAtUtc
            })).ToListAsync(cancellationToken);
        return Ok(new { rows, totalTransactions, truncated = totalTransactions > 10_000 });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("contributions.view")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.DigitalCollections.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.PaymentReference, x.ProviderTransactionId, x.GiverType, x.MemberId,
                x.ProviderEnvironmentSnapshot,
                isSandbox = x.ProviderEnvironmentSnapshot == "SANDBOX",
                contributorName = x.GiverType == DigitalGiverTypes.Anonymous ? "Anonymous" :
                    (x.Member != null ? x.Member.FirstName + " " + x.Member.LastName : x.ContributorNameSnapshot),
                x.Status, x.SettlementStatus, x.CurrencyCode, x.GrossAmount, x.ProviderFee,
                x.NetSettlementAmount, x.FailureReason, x.CreatedDate, x.InitiatedAtUtc,
                x.ProviderCompletedAtUtc, x.SettledAtUtc,
                paymentMethod = x.PaymentMethodDefinition != null ? x.PaymentMethodDefinition.Name : "Unknown",
                provider = x.PaymentProviderConfiguration != null ? x.PaymentProviderConfiguration.DisplayName : null,
                lines = x.Lines.Select(line => new
                {
                    line.Id, contributionType = line.ContributionType != null ? line.ContributionType.Name : "Unknown",
                    line.Amount, line.ContributionId,
                    contributionNumber = line.Contribution != null ? line.Contribution.ContributionNumber : null,
                    destinationType = line.SectionId != null ? "SECTION" : line.GroupId != null ? "GROUP" : line.CommitteeId != null ? "COMMITTEE" : "ORGANIZATION",
                    destination = line.Section != null ? line.Section.SectionName : line.Group != null ? line.Group.GroupName : line.Committee != null ? line.Committee.CommitteeName : "Organization-wide"
                }).ToList(),
                events = x.Events.OrderByDescending(e => e.OccurredAtUtc).Select(e => new
                {
                    e.Id, e.EventType, e.PreviousStatus, e.NewStatus, e.Source, e.CorrelationId, e.OccurredAtUtc
                }).ToList(),
                reversals = db.FinancialAdjustments.Where(r => r.DigitalCollectionId == x.Id)
                    .OrderByDescending(r => r.CreatedDate).Select(r => new
                {
                    r.Id, ReversalStatus = r.Status, r.Reason, requestedById = r.CreatedById,
                    requestedBy = db.Users.Where(u => u.Id == r.CreatedById)
                        .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    r.CreatedDate, ReversedAtUtc = r.CompletedAtUtc, ReversedById = r.CompletedById,
                    reversedBy = db.Users.Where(u => u.Id == r.CompletedById)
                        .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
                    ProviderRefundInitiated = false, ProviderRefundTransactionId = (string?)null
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
        return item is null ? NotFound(new { message = "Digital payment transaction was not found." }) : Ok(item);
    }

    [HttpPost("{id:guid}/reconcile")]
    [RequirePermission("contributions.confirm")]
    public async Task<IActionResult> Reconcile(Guid id, [FromBody] TransactionActionRequest request, CancellationToken cancellationToken)
    {
        var item = await db.DigitalCollections.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound(new { message = "Digital payment transaction was not found." });
        if (item.Status != PaymentTransactionStatuses.Succeeded)
            return Conflict(new { message = "Only a verified successful payment can be reconciled." });
        if (item.SettlementStatus == PaymentSettlementStatuses.Reconciled) return Ok(new { item.Id, item.SettlementStatus });

        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        var previous = item.SettlementStatus;
        item.SettlementStatus = PaymentSettlementStatuses.Reconciled;
        item.SettledAtUtc ??= DateTime.UtcNow;
        item.UpdatedDate = DateTime.UtcNow;
        item.UpdatedById = actor;
        item.Version++;
        db.DigitalCollectionEvents.Add(NewEvent(item.Id, "PAYMENT_RECONCILED", previous, item.SettlementStatus, request.Reason, actor));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { item.Id, item.SettlementStatus });
    }

    [HttpPost("{id:guid}/flag")]
    [RequirePermission("contributions.confirm")]
    public async Task<IActionResult> Flag(Guid id, [FromBody] TransactionActionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "An investigation reason is required." });
        var item = await db.DigitalCollections.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound(new { message = "Digital payment transaction was not found." });
        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        db.DigitalCollectionEvents.Add(NewEvent(item.Id, "INVESTIGATION_FLAGGED", item.Status, item.Status, request.Reason, actor));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { item.Id, flagged = true });
    }

    [HttpPost("{id:guid}/resend-receipt")]
    [RequirePermission("contributions.view")]
    public async Task<IActionResult> ResendReceipt(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.DigitalCollections.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Id, x.PaymentReference, x.Status, x.ProviderEnvironmentSnapshot })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null) return NotFound(new { message = "Digital payment transaction was not found." });
        if (item.Status != PaymentTransactionStatuses.Succeeded)
            return Conflict(new { message = "A final receipt can be sent only for a verified successful payment." });
        var sent = await paymentProcessor.SendReceiptNotificationAsync(item.PaymentReference, cancellationToken);
        if (!sent) return UnprocessableEntity(new { message = "No deliverable email address is available for this contributor, or receipt delivery could not be queued." });
        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        db.DigitalCollectionEvents.Add(NewEvent(item.Id, "RECEIPT_RESEND_REQUESTED", item.Status, item.Status, null, actor));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Receipt delivery was queued." });
    }

    [HttpPost("{id:guid}/refresh-status")]
    [RequirePermission("contributions.confirm")]
    public async Task<IActionResult> RefreshProviderStatus(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.DigitalCollections
            .Include(x => x.PaymentProviderConfiguration)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound(new { message = "Digital payment transaction was not found." });
        if (item.Status is PaymentTransactionStatuses.Succeeded or PaymentTransactionStatuses.Reversed
            or PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Cancelled or PaymentTransactionStatuses.Expired)
            return Conflict(new { message = $"A {item.Status} payment cannot be retried. Start no new charge from this transaction; use the appropriate reconciliation or reversal process." });
        if (item.PaymentProviderConfiguration is null || string.IsNullOrWhiteSpace(item.ProviderTransactionId))
            return Conflict(new { message = "This payment has no provider reference to refresh. It requires investigation." });

        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        var attemptNumber = item.ReconciliationAttemptCount + 1;
        item.ReconciliationAttemptCount = attemptNumber;
        item.LastReconciliationAtUtc = DateTime.UtcNow;
        db.DigitalCollectionEvents.Add(NewEvent(id, "MANUAL_STATUS_QUERY_STARTED", item.Status, item.Status,
            $"Attempt {attemptNumber}: manual provider status refresh", actor));
        try
        {
            var provider = providers.GetRequired(item.PaymentProviderConfiguration.ProviderKey);
            var settings = await providerConfigurations.GetRuntimeSettingsAsync(item.PaymentProviderConfiguration.Id, true, cancellationToken);
            var result = await provider.QueryStatusAsync(item.ProviderTransactionId, settings, cancellationToken);
            var providerStatus = result.Status.Trim().ToUpperInvariant();
            if (providerStatus == PaymentTransactionStatuses.Succeeded)
            {
                await db.SaveChangesAsync(cancellationToken);
                var finalized = await finalization.ApplyVerifiedStatusAsync(id, new ProviderVerifiedPayment(
                    item.ProviderTransactionId, result.Status, result.VerifiedAmount, result.VerifiedCurrencyCode,
                    result.ProviderFee, result.NetSettlementAmount, result.ProviderCompletedAtUtc, result.ProviderMessage),
                    "MANUAL_STATUS_QUERY", HttpContext.TraceIdentifier, actor, cancellationToken);
                if (finalized.Completed)
                    await paymentProcessor.SendReceiptNotificationAsync(finalized.PaymentReference, cancellationToken);
                return Ok(finalized);
            }
            if (providerStatus is not (PaymentTransactionStatuses.Pending or PaymentTransactionStatuses.Processing
                or PaymentTransactionStatuses.Failed or PaymentTransactionStatuses.Cancelled or PaymentTransactionStatuses.Expired or PaymentTransactionStatuses.Unknown))
            {
                db.DigitalCollectionEvents.Add(NewEvent(id, "MANUAL_STATUS_QUERY_REJECTED", item.Status, item.Status,
                    $"Provider returned unsupported status '{providerStatus}'.", actor));
                await db.SaveChangesAsync(cancellationToken);
                return Conflict(new { message = $"Provider returned unsupported status '{providerStatus}'. The transaction was not changed." });
            }

            var previous = item.Status;
            item.Status = providerStatus;
            item.FailureReason = providerStatus == PaymentTransactionStatuses.Failed ? "Provider status query reported failure." : null;
            if (providerStatus is PaymentTransactionStatuses.Pending or PaymentTransactionStatuses.Processing or PaymentTransactionStatuses.Unknown)
            {
                item.ReconciliationStatus = PaymentReconciliationStatuses.Scheduled;
                item.NextReconciliationAtUtc = DateTime.UtcNow.AddSeconds(15);
            }
            else
            {
                item.ReconciliationStatus = PaymentReconciliationStatuses.None;
                item.NextReconciliationAtUtc = null;
            }
            item.UpdatedDate = DateTime.UtcNow; item.UpdatedById = actor; item.Version++;
            db.DigitalCollectionEvents.Add(NewEvent(id, "PROVIDER_STATUS_REFRESHED", previous, item.Status, null, actor));
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { status = item.Status, providerStatus, message = "Provider status refreshed without creating a new charge." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Provider status refresh failed for digital payment {PaymentReference}.", item.PaymentReference);
            db.DigitalCollectionEvents.Add(NewEvent(id, "MANUAL_STATUS_QUERY_FAILED", item.Status, item.Status,
                $"Attempt {attemptNumber} failed; no new charge was created.", actor));
            await db.SaveChangesAsync(cancellationToken);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "The provider status could not be refreshed. No new charge was created." });
        }
    }

    [HttpPost("{id:guid}/reversals")]
    [RequirePermission("contributions.confirm")]
    public async Task<IActionResult> RequestReversal(Guid id, [FromBody] TransactionActionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "A reversal reason is required." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var payment = await db.DigitalCollections.Include(x => x.Lines).ThenInclude(x => x.Contribution)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment is null) return NotFound(new { message = "Digital payment transaction was not found." });
        if (payment.Status != PaymentTransactionStatuses.Succeeded)
            return Conflict(new { message = "Only a verified successful payment can enter the reversal workflow." });
        if (payment.Lines.Count == 0 || payment.Lines.Any(x => x.Contribution is null))
            return Conflict(new { message = "The posted contribution records are incomplete. No reversal request was created." });
        if (payment.Lines.Any(x => x.Contribution!.FinalizedFinancialRecordBookId.HasValue))
            return Conflict(new { message = "A contribution belongs to a finalised Financial Record Book. Use an authorised accounting adjustment instead." });
        var contributionIds = payment.Lines.Select(x => x.ContributionId!.Value).ToArray();
        if (await db.FinancialRecordBookContributions.AnyAsync(x => contributionIds.Contains(x.ContributionId), cancellationToken))
            return Conflict(new { message = "A contribution is attached to a Financial Record Book. Remove it from an editable book before requesting reversal." });
        if (await db.FinancialAdjustments.AnyAsync(x => x.DigitalCollectionId == id &&
                (x.Status == FinancialAdjustmentStatuses.Requested || x.Status == FinancialAdjustmentStatuses.Approved), cancellationToken))
            return Conflict(new { message = "This payment already has an active or approved reversal." });

        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        var now = DateTime.UtcNow;
        var reversal = new FinancialAdjustment
        {
            Id = Guid.NewGuid(), DigitalCollectionId = id, Reason = request.Reason.Trim(),
            AdjustmentType = FinancialAdjustmentTypes.AccountingReversal,
            Status = FinancialAdjustmentStatuses.Requested,
            CreatedById = actor, UpdatedById = actor, CreatedDate = now, UpdatedDate = now, Version = 1
        };
        db.FinancialAdjustments.Add(reversal);
        payment.UpdatedById = actor; payment.UpdatedDate = now; payment.Version++;
        db.DigitalCollectionEvents.Add(NewEvent(id, "REVERSAL_REQUESTED", payment.Status, payment.Status,
            "Internal accounting reversal requested. No provider refund was initiated. " + request.Reason.Trim(), actor));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Accepted(new { reversal.Id, ReversalStatus = reversal.Status, message = "Reversal submitted for independent approval. No provider refund has been initiated." });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This transaction changed while the reversal was requested. Reload and try again." });
        }
    }

    [HttpPost("{id:guid}/reversals/{reversalId:guid}/approve")]
    [RequirePermission("contributions.confirm")]
    public async Task<IActionResult> ApproveReversal(Guid id, Guid reversalId, [FromBody] TransactionActionRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        var reversal = await db.FinancialAdjustments.SingleOrDefaultAsync(x => x.Id == reversalId && x.DigitalCollectionId == id, cancellationToken);
        if (reversal is null) return NotFound(new { message = "Reversal request was not found." });
        if (reversal.Status != FinancialAdjustmentStatuses.Requested)
            return Conflict(new { message = $"A {reversal.Status} reversal cannot be approved." });
        if (reversal.CreatedById == actor)
            return Conflict(new { message = "The maker cannot approve their own reversal request. Another authorised user must act as checker." });

        var payment = await db.DigitalCollections.Include(x => x.Lines).ThenInclude(x => x.Contribution)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payment is null) return NotFound(new { message = "Digital payment transaction was not found." });
        if (payment.Status != PaymentTransactionStatuses.Succeeded)
            return Conflict(new { message = "Only a verified successful payment can be reversed." });
        var contributions = payment.Lines.Select(x => x.Contribution).Where(x => x is not null).Cast<Contribution>().ToList();
        if (contributions.Count != payment.Lines.Count)
            return Conflict(new { message = "The posted contribution records are incomplete. No balances were changed." });
        if (contributions.Any(x => x.IsArchived))
            return Conflict(new { message = "One or more contribution records are already archived. No balances were changed." });
        if (contributions.Any(x => x.FinalizedFinancialRecordBookId.HasValue))
            return Conflict(new { message = "A contribution belongs to a finalised Financial Record Book. Use an authorised accounting adjustment instead." });

        var contributionIds = contributions.Select(x => x.Id).ToArray();
        if (await db.FinancialRecordBookContributions.AnyAsync(x => contributionIds.Contains(x.ContributionId), cancellationToken))
            return Conflict(new { message = "A contribution is already attached to a Financial Record Book. Remove it from an editable book, or use an authorised adjustment if the book is finalised." });
        var postings = await db.FundTransactions
            .Where(x => x.ReferenceType == "CONTRIBUTION" && x.ReferenceId.HasValue && contributionIds.Contains(x.ReferenceId.Value) && !x.IsReversed)
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.Id).ToListAsync(cancellationToken);
        if (postings.Count == 0 || contributions.Any(c => postings.Where(p => p.ReferenceId == c.Id).Sum(p => p.Amount) != c.Amount))
            return Conflict(new { message = "The original ledger postings could not be matched exactly. No balances were changed; investigate the transaction before retrying." });

        var now = DateTime.UtcNow;
        var reversalPostingIds = new List<Guid>();
        foreach (var posting in postings)
        {
            var contribution = contributions.Single(x => x.Id == posting.ReferenceId);
            var balance = db.FundBalances.Local.FirstOrDefault(x => x.FiscalYear == contribution.ContributionDate.Year &&
                    x.SectionId == posting.SectionId && x.GroupId == posting.GroupId && x.CommitteeId == posting.CommitteeId)
                ?? await db.FundBalances.FirstOrDefaultAsync(x => x.FiscalYear == contribution.ContributionDate.Year &&
                    x.SectionId == posting.SectionId && x.GroupId == posting.GroupId && x.CommitteeId == posting.CommitteeId, cancellationToken);
            if (balance is null)
                return Conflict(new { message = $"The fund balance for ledger posting {posting.Id} could not be found. No reversal was applied." });
            if (balance.OwnFundsBalance < posting.Amount)
                return Conflict(new { message = "Available own funds are insufficient for this reversal. Use the authorised accounting adjustment process." });

            var previous = balance.OwnFundsBalance;
            balance.OwnFundsBalance -= posting.Amount;
            balance.UpdatedById = actor; balance.UpdatedDate = now; balance.Version++;
            posting.IsReversed = true; posting.ReversedBy = actor; posting.ReversedAt = now;
            posting.ReversalReason = reversal.Reason; posting.UpdatedById = actor; posting.UpdatedDate = now; posting.Version++;
            var reversalPostingId = Guid.NewGuid(); reversalPostingIds.Add(reversalPostingId);
            db.FundTransactions.Add(new FundTransaction
            {
                Id = reversalPostingId, SectionId = posting.SectionId, GroupId = posting.GroupId, CommitteeId = posting.CommitteeId,
                TransactionType = "SUBTRACT", FundType = posting.FundType, Amount = -posting.Amount,
                PreviousBalance = previous, NewBalance = balance.OwnFundsBalance,
                Description = $"Digital Giving reversal: {payment.PaymentReference}", Source = "DIGITAL_GIVING_REVERSAL",
                ReferenceType = "DIGITAL_GIVING_REVERSAL", ReferenceId = reversal.Id,
                Metadata = JsonSerializer.Serialize(new { OriginalFundTransactionId = posting.Id, ContributionId = contribution.Id, reversal.Reason }),
                CreatedById = actor, UpdatedById = actor, CreatedDate = now, UpdatedDate = now, Version = 1
            });
        }

        foreach (var contribution in contributions)
        {
            contribution.IsArchived = true; contribution.ArchivedAt = now; contribution.ArchivedBy = actor;
            contribution.ArchiveReason = $"Digital payment reversal {reversal.Id}: {reversal.Reason}";
            contribution.UpdatedById = actor; contribution.UpdatedDate = now; contribution.Version++;
        }
        reversal.Status = FinancialAdjustmentStatuses.Approved;
        reversal.CompletedAtUtc = now; reversal.CompletedById = actor;
        reversal.UpdatedById = actor; reversal.UpdatedDate = now; reversal.Version++;
        var previousStatus = payment.Status;
        payment.Status = PaymentTransactionStatuses.Reversed; payment.UpdatedById = actor; payment.UpdatedDate = now; payment.Version++;
        var overview = await db.OrganizationOverviews.FirstOrDefaultAsync(cancellationToken);
        if (overview is not null) { overview.IsStale = true; overview.UpdatedById = actor; overview.UpdatedDate = now; overview.Version++; }
        db.DigitalCollectionEvents.Add(NewEvent(id, "ACCOUNTING_REVERSAL_APPROVED", previousStatus, payment.Status,
            $"Internal accounting reversal approved. Provider refund initiated: false. Reversal postings: {string.Join(',', reversalPostingIds)}. " + (request.Reason ?? string.Empty), actor));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(new { reversal.Id, ReversalStatus = reversal.Status, payment.Status, providerRefundInitiated = false, message = "The internal accounting reversal was completed. No provider refund was initiated." });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This transaction changed during approval. No partial reversal was committed; reload and try again." });
        }
    }

    [HttpPost("{id:guid}/reversals/{reversalId:guid}/reject")]
    [RequirePermission("contributions.confirm")]
    public async Task<IActionResult> RejectReversal(Guid id, Guid reversalId, [FromBody] TransactionActionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "A rejection reason is required." });
        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        var reversal = await db.FinancialAdjustments.SingleOrDefaultAsync(x => x.Id == reversalId && x.DigitalCollectionId == id, cancellationToken);
        if (reversal is null) return NotFound(new { message = "Reversal request was not found." });
        if (reversal.Status != FinancialAdjustmentStatuses.Requested)
            return Conflict(new { message = $"A {reversal.Status} reversal cannot be rejected." });
        if (reversal.CreatedById == actor)
            return Conflict(new { message = "The maker cannot review their own reversal request. Another authorised user must act as checker." });
        var payment = await db.DigitalCollections.SingleAsync(x => x.Id == id, cancellationToken);
        reversal.Status = FinancialAdjustmentStatuses.Rejected;
        reversal.UpdatedById = actor; reversal.UpdatedDate = DateTime.UtcNow; reversal.Version++;
        payment.UpdatedById = actor; payment.UpdatedDate = DateTime.UtcNow; payment.Version++;
        db.DigitalCollectionEvents.Add(NewEvent(id, "REVERSAL_REJECTED", payment.Status, payment.Status, request.Reason.Trim(), actor));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { reversal.Id, ReversalStatus = reversal.Status, message = "Reversal request rejected. No balances or provider funds were changed." });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This reversal changed while it was being reviewed. Reload and try again." });
        }
    }

    private static IQueryable<DigitalCollection> ApplyPeriod(IQueryable<DigitalCollection> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue) query = query.Where(x => x.CreatedDate >= DateTime.SpecifyKind(from.Value, DateTimeKind.Utc));
        if (to.HasValue) query = query.Where(x => x.CreatedDate < DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc));
        return query;
    }

    private DigitalCollectionEvent NewEvent(Guid transactionId, string type, string? previous, string next, string? reason, Guid actor) => new()
    {
        Id = Guid.NewGuid(), DigitalCollectionId = transactionId, EventType = type,
        PreviousStatus = previous, NewStatus = next, Source = "ADMIN_API",
        CorrelationId = $"actor:{actor:N}", ProtectedDetails = string.IsNullOrWhiteSpace(reason) ? null : _eventProtector.Protect(reason.Trim()),
        OccurredAtUtc = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow,
        CreatedById = actor, UpdatedById = actor, Version = 1
    };
}

public sealed record TransactionActionRequest(string? Reason);
