using System.Text.Json;
using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/reports/revenue"), Authorize, RequirePermission("reports.view")]
public sealed class RevenueReportsController(AppDbContext db, ICurrentUserService current, ISystemLogWriter logs,
    IGroupAccessService access, IWorkflowAuthorizationService workflow) : ControllerBase
{
    [HttpGet("consolidated-income")]
    public async Task<IActionResult> ConsolidatedIncome([FromQuery] DateTime? from, [FromQuery] DateTime to,
        [FromQuery] Guid? sectionId, [FromQuery] Guid? groupId, [FromQuery] Guid? committeeId, CancellationToken ct = default)
    {
        var startDate = from?.Date ?? new DateTime(2000, 1, 1);
        if (startDate > to.Date) return BadRequest(new { message = "The report start date must be on or before its end date." });
        var start = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
        var scope = await Scope(ct);
        if (new[] { sectionId, groupId, committeeId }.Count(x => x.HasValue) > 1)
            return BadRequest(new { message = "Select only one organizational scope." });
        var contributionQuery = db.Contributions.AsNoTracking().Include(x => x.ContributionType).Include(x => x.Allocations).ThenInclude(x => x.ContributionType)
            .Where(x => !x.IsArchived && x.ReversesContributionId == null &&
                (x.Status == "CONFIRMED" || x.Status == "ALLOCATED") && x.ContributionDate >= start && x.ContributionDate < end)
            .ApplyScope(scope);
        if (sectionId.HasValue) contributionQuery = contributionQuery.Where(x => x.Allocations.Any(a => a.SectionId == sectionId));
        if (groupId.HasValue) contributionQuery = contributionQuery.Where(x => x.Allocations.Any(a => a.GroupId == groupId));
        if (committeeId.HasValue) contributionQuery = contributionQuery.Where(x => x.Allocations.Any(a => a.CommitteeId == committeeId));
        var contributions = await contributionQuery.ToListAsync(ct);
        var directRows = contributions.SelectMany(x => x.Allocations.Count > 0
            ? x.Allocations.Select(a => new { Id = a.Id, DocumentId = x.Id, Reference = x.ContributionNumber, Date = x.ContributionDate,
                ContributionType = a.ContributionType?.Name ?? x.ContributionType!.Name, Amount = a.AllocatedAmount,
                SourceModule = x.OriginType == "SERVICE_COLLECTION" ? "SERVICE_COLLECTION" : "CONTRIBUTION", x.IsArchived })
            : [new { Id = x.Id, DocumentId = x.Id, Reference = x.ContributionNumber, Date = x.ContributionDate,
                ContributionType = x.ContributionType!.Name, Amount = x.Amount,
                SourceModule = x.OriginType == "SERVICE_COLLECTION" ? "SERVICE_COLLECTION" : "CONTRIBUTION", x.IsArchived }]).ToList();
        var rows = directRows.OrderBy(x => x.Date).ThenBy(x => x.Reference).ToList();
        await Audit("ConsolidatedIncomeViewed", new { from = start, to = end, sectionId, groupId, committeeId,
            ContributionLines = directRows.Count }, ct);
        return Ok(new { From = start.Date, To = end.AddDays(-1).Date,
            DirectContributions = directRows.Where(x => x.SourceModule == "CONTRIBUTION").Sum(x => x.Amount),
            ServiceCollections = rows.Where(x => x.SourceModule == "SERVICE_COLLECTION").Sum(x => x.Amount),
            Total = rows.Sum(x => x.Amount), Rows = rows });
    }

    [HttpGet("service-collections")]
    public async Task<IActionResult> ServiceCollections([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] bool onlyExceptions = false, CancellationToken ct = default)
    {
        var (start, end) = Period(from, to); var query = db.ServiceCollections.AsNoTracking().Include(x => x.ServiceEvent).ThenInclude(x => x!.ServiceType)
            .Where(x => x.ServiceEvent!.StartsAtUtc >= start && x.ServiceEvent.StartsAtUtc < end);
        var scope = await Scope(ct);
        if (!scope.Global) query = query.Where(x => x.SectionId.HasValue && scope.Sections.Contains(x.SectionId.Value) ||
            x.GroupId.HasValue && scope.Groups.Contains(x.GroupId.Value) || x.CommitteeId.HasValue && scope.Committees.Contains(x.CommitteeId.Value));
        if (onlyExceptions) query = query.Where(x => x.Variance != 0 || x.Status == ServiceCollectionStatuses.Returned);
        var rows = await query.OrderBy(x => x.ServiceEvent!.StartsAtUtc).Select(x => new { x.Id, x.CollectionNumber,
            x.ServiceEvent!.StartsAtUtc, ServiceType = x.ServiceEvent.ServiceType!.Name, x.Status, x.PurposeTotal, x.TenderTotal, x.Variance,
            x.VarianceExplanation, x.ReviewComments, x.FinalizedFinancialRecordBookId, EntryCount = x.Entries.Count }).ToListAsync(ct);
        await Audit("ServiceCollectionReconciliationViewed", new { from = start, to = end, onlyExceptions, Count = rows.Count }, ct);
        return Ok(new { From = start.Date, To = end.AddDays(-1).Date, PurposeTotal = rows.Sum(x => x.PurposeTotal), TenderTotal = rows.Sum(x => x.TenderTotal), Variance = rows.Sum(x => x.Variance), Rows = rows });
    }

    [HttpGet("pending-frb")]
    public async Task<IActionResult> PendingFrb([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
    {
        var (start, end) = Period(from, to);
        var scope = await Scope(ct);
        var direct = await db.Contributions.AsNoTracking().Include(x => x.ContributionType).Where(x => !x.IsArchived && x.ReversesContributionId == null && x.OriginType != "SERVICE_COLLECTION" &&
                (x.Status == "CONFIRMED" || x.Status == "ALLOCATED") && x.ContributionDate >= start && x.ContributionDate < end && x.FinalizedFinancialRecordBookId == null)
            .ApplyScope(scope).OrderBy(x => x.ContributionDate).Select(x => new { x.Id, Reference = x.ContributionNumber, Date = x.ContributionDate,
                Description = x.ContributionType!.Name, x.Amount, SourceModule = "CONTRIBUTION" }).ToListAsync(ct);
        var collectionQuery = db.ServiceCollections.AsNoTracking().Include(x => x.ServiceEvent).Where(x => x.Status == ServiceCollectionStatuses.Approved &&
            x.ServiceEvent!.StartsAtUtc >= start && x.ServiceEvent.StartsAtUtc < end && x.FinalizedFinancialRecordBookId == null);
        if (!scope.Global) collectionQuery = collectionQuery.Where(x => x.SectionId.HasValue && scope.Sections.Contains(x.SectionId.Value) ||
            x.GroupId.HasValue && scope.Groups.Contains(x.GroupId.Value) || x.CommitteeId.HasValue && scope.Committees.Contains(x.CommitteeId.Value));
        var collections = await collectionQuery.Select(x => new { x.Id, Reference = x.CollectionNumber, Date = x.ServiceEvent!.StartsAtUtc,
            Description = "Service Collection", Amount = x.PurposeTotal, SourceModule = "SERVICE_COLLECTION" }).ToListAsync(ct);
        var rows = direct.Concat(collections).OrderBy(x => x.Date).ToList();
        await Audit("PendingFrbRevenueViewed", new { from = start, to = end, DirectCount = direct.Count, ServiceCollectionCount = collections.Count }, ct);
        return Ok(new { From = start.Date, To = end.AddDays(-1).Date, Total = rows.Sum(x => x.Amount), Rows = rows });
    }

    [HttpGet("asset-hire")]
    public async Task<IActionResult> AssetHire([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
    {
        var (start, end) = Period(from, to);
        var rows = await db.Contributions.AsNoTracking().Where(x => x.OriginType == "ASSET_HIRE" && x.ContributionDate >= start && x.ContributionDate < end)
            .ApplyScope(await Scope(ct))
            .OrderBy(x => x.ContributionDate).Select(x => new { x.Id, x.ContributionNumber, x.ContributionDate, x.Amount, x.Status,
                x.ReferenceNumber, x.AssetCheckoutId, x.FinalizedFinancialRecordBookId, x.ContributorNameSnapshot }).ToListAsync(ct);
        await Audit("AssetHireRevenueViewed", new { from = start, to = end, Count = rows.Count }, ct);
        return Ok(new { From = start.Date, To = end.AddDays(-1).Date, Total = rows.Sum(x => x.Amount), Rows = rows });
    }

    [HttpGet("fcy-register")]
    public async Task<IActionResult> FcyRegister([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct = default)
    {
        var (start, end) = Period(from, to);
        var rows = await db.ServiceCollectionTenders.AsNoTracking().Include(x => x.ServiceCollection)
            .Where(x => x.ServiceCollection!.ServiceEvent!.StartsAtUtc >= start && x.ServiceCollection.ServiceEvent.StartsAtUtc < end && x.CurrencyCode != x.ServiceCollection.BaseCurrencyCode)
            .ApplyScope(await Scope(ct))
            .OrderBy(x => x.ServiceCollection!.CreatedDate).Select(x => new { x.ServiceCollectionId, x.ServiceCollection!.CollectionNumber,
                x.CurrencyCode, x.OriginalAmount, x.ExchangeRate, x.BaseEquivalent, x.ExchangeRateId, x.IsManualRateOverride, x.OverrideReason, x.ServiceCollection.Status }).ToListAsync(ct);
        await Audit("FcyConversionRegisterViewed", new { from = start, to = end, Count = rows.Count }, ct);
        return Ok(new { From = start.Date, To = end.AddDays(-1).Date, Rows = rows });
    }

    private static (DateTime Start, DateTime End) Period(DateTime from, DateTime to)
    {
        if (from.Date > to.Date || (to.Date - from.Date).TotalDays > 3660) throw new ArgumentException("Select a valid reporting period of no more than ten years.");
        return (DateTime.SpecifyKind(from.Date, DateTimeKind.Utc), DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc));
    }
    private Task Audit(string action, object details, CancellationToken ct) => logs.WriteAsync(new SystemLog { Category = "Audit", Process = "Revenue Reporting",
        Action = action, UserId = current.GetUserId(), CorrelationId = HttpContext.TraceIdentifier, Message = action, Details = JsonSerializer.Serialize(details) }, ct);
    private async Task<ReportScope> Scope(CancellationToken ct)
    {
        var userId = current.GetUserId();
        var global = access.HasGlobalAccess() || userId.HasValue && await workflow.HasCongregationScopeAsync(userId.Value, ct);
        return global ? new(true, [], [], []) : new(false, await access.GetAccessibleSectionIdsAsync(),
            await access.GetAccessibleGroupIdsAsync(), await access.GetAccessibleCommitteeIdsAsync());
    }
}

internal sealed record ReportScope(bool Global, List<Guid> Sections, List<Guid> Groups, List<Guid> Committees);

internal static class RevenueReportScopeExtensions
{
    public static IQueryable<Contribution> ApplyScope(this IQueryable<Contribution> query, ReportScope scope) => scope.Global ? query :
        query.Where(x => x.Allocations.Any(a => a.SectionId.HasValue && scope.Sections.Contains(a.SectionId.Value) ||
            a.GroupId.HasValue && scope.Groups.Contains(a.GroupId.Value) || a.CommitteeId.HasValue && scope.Committees.Contains(a.CommitteeId.Value)));
    public static IQueryable<ServiceCollectionTender> ApplyScope(this IQueryable<ServiceCollectionTender> query, ReportScope scope) => scope.Global ? query :
        query.Where(x => x.ServiceCollection!.SectionId.HasValue && scope.Sections.Contains(x.ServiceCollection.SectionId.Value) ||
            x.ServiceCollection.GroupId.HasValue && scope.Groups.Contains(x.ServiceCollection.GroupId.Value) ||
            x.ServiceCollection.CommitteeId.HasValue && scope.Committees.Contains(x.ServiceCollection.CommitteeId.Value));
}
