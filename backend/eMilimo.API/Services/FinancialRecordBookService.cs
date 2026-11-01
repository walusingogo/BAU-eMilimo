using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public sealed class FinancialRecordBookService(
    AppDbContext db, IReferenceNumberService references, IGroupAccessService groupAccess, ICurrentUserService currentUser,
    IWorkflowAuthorizationService workflow) : IFinancialRecordBookService
{
    private static decimal Money(decimal value) => FrbFinancialCalculator.Money(value);

    public async Task<object> ListAsync(string? status, string? search, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = await ScopedAsync(db.FinancialRecordBooks.AsNoTracking(), ct);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.ToUpper());
        else query = query.Where(x => x.Status != FinancialRecordBookStatuses.Archived);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FrbNumber.Contains(search));
        if (from.HasValue) { var day = DateOnly.FromDateTime(from.Value); query = query.Where(x => x.PeriodEnd >= day); }
        if (to.HasValue) { var day = DateOnly.FromDateTime(to.Value); query = query.Where(x => x.PeriodStart <= day); }
        var rows = await query.OrderByDescending(x => x.CreatedDate).Select(x => new
        {
            x.Id, x.FrbNumber, x.PeriodStart, x.PeriodEnd, x.Status, x.GrossContributions,
            x.AssessmentTotal, x.NetPosition, x.CreatedById, x.CreatedDate,
            CreatedBy = db.Users.Where(u => u.Id == x.CreatedById).Select(u => (u.FirstName + " " + u.LastName).Trim()).FirstOrDefault(),
            Section = x.Section == null ? null : x.Section.SectionName,
            VerificationCount = x.Verifications.Count
        }).ToListAsync(ct);
        return new { data = rows, total = rows.Count };
    }

    public async Task<object?> GetAsync(Guid id, CancellationToken ct)
    {
        var frb = await Full(await ScopedAsync(db.FinancialRecordBooks.AsNoTracking(), ct)).FirstOrDefaultAsync(x => x.Id == id, ct);
        return frb == null ? null : await PresentAsync(frb, ct);
    }

    public async Task<object> EligibleContributionsAsync(DateTime from, DateTime to, Guid? sectionId, Guid? contributionTypeId, string? search, CancellationToken ct)
    {
        ValidatePeriod(from, to);
        if (sectionId.HasValue) await DemandSectionAccessAsync(sectionId.Value, ct);
        var (startUtc, endExclusiveUtc) = FrbQueryDateRange.InclusiveDays(from, to);
        var query = db.Contributions.AsNoTracking().Include(x => x.ContributionType)
            .Where(x => !x.IsArchived && x.ReversesContributionId == null &&
                        x.OriginType != "SERVICE_COLLECTION" && (x.Status == "CONFIRMED" || x.Status == "ALLOCATED") &&
                        x.ContributionDate >= startUtc && x.ContributionDate < endExclusiveUtc &&
                        x.FinalizedFinancialRecordBookId == null);
        if (sectionId.HasValue) query = query.Where(x => x.Allocations.Any(a => a.SectionId == sectionId));
        else if (!groupAccess.HasGlobalAccess())
        {
            var groups = await groupAccess.GetAccessibleGroupIdsAsync();
            var sections = await groupAccess.GetAccessibleSectionIdsAsync();
            var committees = await groupAccess.GetAccessibleCommitteeIdsAsync();
            query = query.Where(x => x.Allocations.Any(a =>
                (a.GroupId.HasValue && groups.Contains(a.GroupId.Value)) ||
                (a.SectionId.HasValue && sections.Contains(a.SectionId.Value)) ||
                (a.CommitteeId.HasValue && committees.Contains(a.CommitteeId.Value))));
        }
        if (contributionTypeId.HasValue) query = query.Where(x => x.Allocations.Any(a => a.ContributionTypeId == contributionTypeId) ||
            !x.Allocations.Any() && x.ContributionTypeId == contributionTypeId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.ContributionNumber.Contains(search) || (x.Description ?? "").Contains(search));
        return await query.OrderBy(x => x.ContributionDate).ThenBy(x => x.ContributionNumber).Select(x => new
        {
            x.Id, x.ContributionNumber, x.ContributionDate, ContributionType = x.ContributionType!.Name,
            x.ContributionTypeId, x.Description, x.Amount, Status = x.IsArchived ? "ARCHIVED" : x.Status, x.OriginType, x.AssetCheckoutId,
            ServiceCollectionId = (Guid?)null, ServiceCollectionNumber = (string?)null,
            ContributionTypes = x.Allocations.Select(a => new
            {
                ContributionTypeId = a.ContributionTypeId ?? x.ContributionTypeId,
                ContributionType = a.ContributionType != null ? a.ContributionType.Name : x.ContributionType!.Name,
                Amount = a.AllocatedAmount
            })
        }).ToListAsync(ct);
    }

    public async Task<object> EligibleServiceCollectionsAsync(DateTime from, DateTime to, Guid? sectionId, Guid? contributionTypeId, string? search, CancellationToken ct)
    {
        ValidatePeriod(from, to);
        if (sectionId.HasValue) await DemandSectionAccessAsync(sectionId.Value, ct);
        var (startUtc, endExclusiveUtc) = FrbQueryDateRange.InclusiveDays(from, to);
        var query = db.ServiceCollections.AsNoTracking().Include(x => x.ServiceEvent).ThenInclude(x => x!.ServiceType)
            .Where(x => x.Status == ServiceCollectionStatuses.Approved && x.FinalizedFinancialRecordBookId == null &&
                x.ServiceEvent!.StartsAtUtc >= startUtc && x.ServiceEvent.StartsAtUtc < endExclusiveUtc);
        if (sectionId.HasValue) query = query.Where(x => x.SectionId == sectionId);
        else if (!groupAccess.HasGlobalAccess())
        {
            var groups = await groupAccess.GetAccessibleGroupIdsAsync(); var sections = await groupAccess.GetAccessibleSectionIdsAsync();
            var committees = await groupAccess.GetAccessibleCommitteeIdsAsync();
            query = query.Where(x => x.SectionId.HasValue && sections.Contains(x.SectionId.Value) ||
                x.GroupId.HasValue && groups.Contains(x.GroupId.Value) || x.CommitteeId.HasValue && committees.Contains(x.CommitteeId.Value));
        }
        if (contributionTypeId.HasValue) query = query.Where(x => x.Entries.Any(e => e.Lines.Any(l => l.ContributionTypeId == contributionTypeId)));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.CollectionNumber.Contains(search) || (x.ServiceEvent!.SequenceName ?? "").Contains(search));
        return await query.OrderBy(x => x.ServiceEvent!.StartsAtUtc).ThenBy(x => x.CollectionNumber).Select(x => new
        {
            x.Id, x.CollectionNumber, ServiceDate = x.ServiceEvent!.StartsAtUtc, Service = x.ServiceEvent.ServiceType!.Name,
            x.PurposeTotal, x.Status, EntryCount = x.Entries.Count,
            ContributionTypes = x.Entries.SelectMany(e => e.Lines).GroupBy(l => new { l.ContributionTypeId, l.ContributionType!.Name })
                .Select(g => new { g.Key.ContributionTypeId, ContributionType = g.Key.Name, Amount = g.Sum(l => l.Amount) })
        }).ToListAsync(ct);
    }

    public async Task<object> PreviewAsync(SaveFinancialRecordBookRequest request, CancellationToken ct)
    {
        ValidatePeriod(request.PeriodStart, request.PeriodEnd);
        var selectedIds = request.ContributionIds.Distinct().ToArray();
        var selectedCollectionIds = request.ServiceCollectionIds?.Distinct().ToArray() ?? [];
        if (selectedIds.Length == 0 && selectedCollectionIds.Length == 0)
            throw new InvalidOperationException("Select at least one eligible Contribution or Service Collection to calculate the assessment preview.");
        if (request.SectionId.HasValue) await DemandSectionAccessAsync(request.SectionId.Value, ct);
        else if (!groupAccess.HasGlobalAccess()) throw new UnauthorizedAccessException("Select a Section assigned to you.");

        var preview = new FinancialRecordBook
        {
            Id = Guid.NewGuid(), PeriodStart = DateOnly.FromDateTime(request.PeriodStart),
            PeriodEnd = DateOnly.FromDateTime(request.PeriodEnd), SectionId = request.SectionId,
            AssessmentRuleIdsJson = SerializeRuleSelection(request.AssessmentRuleIds),
            Contributions = selectedIds.Select(id => new FinancialRecordBookContribution
                { Id = Guid.NewGuid(), ContributionId = id }).ToList(),
            ServiceCollections = selectedCollectionIds.Select(id => new FinancialRecordBookServiceCollection
                { Id = Guid.NewGuid(), ServiceCollectionId = id }).ToList()
        };
        await EnsureEligibleAsync(preview, selectedIds, ct);
        await EnsureServiceCollectionsEligibleAsync(preview, selectedCollectionIds, ct);
        var serviceContributionIds = await ServiceCollectionContributionIdsAsync(preview, selectedCollectionIds, ct);
        foreach (var id in serviceContributionIds.Where(id => preview.Contributions.All(x => x.ContributionId != id)))
            preview.Contributions.Add(new FinancialRecordBookContribution { Id = Guid.NewGuid(), ContributionId = id });
        var calculations = await CalculateAsync(preview, useSnapshots: false, ct);
        return new
        {
            GrossContributions = calculations.Gross,
            calculations.AssessmentTotal,
            NetPosition = calculations.Net,
            Assessments = calculations.Assessments.Select(x => new
            {
                x.Snapshot.SourceRuleId, x.Snapshot.RuleName, x.Snapshot.Classification,
                x.Snapshot.CalculationMethod, x.Snapshot.AssessmentBase, x.Snapshot.CalculatedAmount,
                ContributionTypeIds = JsonSerializer.Deserialize<Guid[]>(x.Snapshot.ContributionTypeIdsJson) ?? [],
                ContributionTypeNames = JsonSerializer.Deserialize<string[]>(x.Snapshot.ContributionTypeNamesJson) ?? [],
                AllocationLines = SnapshotLines(x.Snapshot).OrderBy(line => line.DisplayOrder).Select(line => new
                    { line.Beneficiary, line.Rate, line.FixedAmount, line.CalculatedAmount, line.IsLocalRetained, line.DisplayOrder, line.Notes })
            })
        };
    }

    public async Task<object> CreateAsync(SaveFinancialRecordBookRequest request, Guid actorId, CancellationToken ct)
    {
        ValidatePeriod(request.PeriodStart, request.PeriodEnd);
        ValidateNotes(request.Notes);
        if (request.SectionId.HasValue) await DemandSectionAccessAsync(request.SectionId.Value, ct);
        else if (!groupAccess.HasGlobalAccess()) throw new UnauthorizedAccessException("Select a Section assigned to you.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var frb = new FinancialRecordBook
        {
            Id = Guid.NewGuid(), FrbNumber = await references.NextAsync("FRB", cancellationToken: ct),
            PeriodStart = DateOnly.FromDateTime(request.PeriodStart), PeriodEnd = DateOnly.FromDateTime(request.PeriodEnd),
            SectionId = request.SectionId, Notes = Clean(request.Notes), Status = FinancialRecordBookStatuses.Draft,
            AssessmentRuleIdsJson = SerializeRuleSelection(request.AssessmentRuleIds),
            CreatedById = actorId, UpdatedById = actorId, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
        };
        db.FinancialRecordBooks.Add(frb);
        await ReplaceContributionsAsync(frb, request.ContributionIds, actorId, ct);
        await ReplaceServiceCollectionsAsync(frb, request.ServiceCollectionIds ?? [], actorId, ct);
        AddAudit(frb.Id, "ASSESSMENTS_SELECTED", new { assessmentRuleIds = request.AssessmentRuleIds?.Distinct().OrderBy(x => x).ToArray() ?? [] }, actorId);
        await RecalculateDraftAsync(frb, ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (await GetAsync(frb.Id, ct))!;
    }

    public async Task<object> UpdateAsync(Guid id, SaveFinancialRecordBookRequest request, Guid actorId, CancellationToken ct)
    {
        ValidatePeriod(request.PeriodStart, request.PeriodEnd);
        ValidateNotes(request.Notes);
        var frb = await db.FinancialRecordBooks.Include(x => x.Contributions).Include(x => x.ServiceCollections).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Financial Record Book not found.");
        await DemandAccessAsync(frb, ct);
        if (frb.Status is not FinancialRecordBookStatuses.Draft and not FinancialRecordBookStatuses.Returned)
            throw new InvalidOperationException("Only a Draft or Returned FRB can be edited.");
        if (request.SectionId.HasValue) await DemandSectionAccessAsync(request.SectionId.Value, ct);
        else if (!groupAccess.HasGlobalAccess()) throw new UnauthorizedAccessException("Select a Section assigned to you.");
        frb.PeriodStart = DateOnly.FromDateTime(request.PeriodStart); frb.PeriodEnd = DateOnly.FromDateTime(request.PeriodEnd);
        frb.SectionId = request.SectionId; frb.Notes = Clean(request.Notes);
        frb.AssessmentRuleIdsJson = SerializeRuleSelection(request.AssessmentRuleIds); frb.UpdatedById = actorId;
        frb.UpdatedDate = DateTime.UtcNow; frb.Version++;
        await ReplaceContributionsAsync(frb, request.ContributionIds, actorId, ct);
        await ReplaceServiceCollectionsAsync(frb, request.ServiceCollectionIds ?? [], actorId, ct);
        AddAudit(frb.Id, "ASSESSMENTS_SELECTED", new { assessmentRuleIds = request.AssessmentRuleIds?.Distinct().OrderBy(x => x).ToArray() ?? [] }, actorId);
        await RecalculateDraftAsync(frb, ct);
        await db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<object> SubmitAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var frb = await db.FinancialRecordBooks.Include(x => x.Contributions).ThenInclude(x => x.Contribution)
            .Include(x => x.ServiceCollections).ThenInclude(x => x.ServiceCollection)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Financial Record Book not found.");
        await DemandAccessAsync(frb, ct);
        if (frb.Status is not FinancialRecordBookStatuses.Draft and not FinancialRecordBookStatuses.Returned)
            throw new InvalidOperationException("Only a Draft or Returned FRB can be submitted.");
        if (frb.Contributions.Count == 0 && frb.ServiceCollections.Count == 0) throw new InvalidOperationException("Select at least one eligible Contribution or Service Collection before submission.");
        if (frb.AssessmentRuleIdsJson == null)
            throw new InvalidOperationException("Edit this FRB and explicitly select at least one applicable assessment before submission.");
        await EnsureEligibleAsync(frb, frb.Contributions.Select(x => x.ContributionId), ct);
        await EnsureServiceCollectionsEligibleAsync(frb, frb.ServiceCollections.Select(x => x.ServiceCollectionId), ct);
        await SnapshotContributionsAsync(frb, ct);
        await SnapshotServiceCollectionsAsync(frb, ct);
        var calculations = await CalculateAsync(frb, useSnapshots: false, ct);
        db.FrbAssessmentSnapshots.RemoveRange(await db.FrbAssessmentSnapshots.Where(x => x.FinancialRecordBookId == id).ToListAsync(ct));
        db.FrbAssessmentSnapshots.AddRange(calculations.Assessments.Select(x => x.Snapshot));
        ApplyTotals(frb, calculations);
        frb.Status = FinancialRecordBookStatuses.AwaitingVerification; frb.SubmittedAtUtc = DateTime.UtcNow;
        frb.SubmittedById = actorId; frb.UpdatedById = actorId; frb.UpdatedDate = DateTime.UtcNow; frb.Version++;
        await db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<object> ArchiveAsync(Guid id, ArchiveFinancialRecordBookRequest request, Guid actorId, CancellationToken ct)
    {
        var reason = Clean(request.Reason);
        if (reason == null) throw new InvalidOperationException("Provide a reason for archiving this FRB.");
        if (reason.Length > 1000) throw new InvalidOperationException("Archive reason cannot exceed 1,000 characters.");
        var frb = await db.FinancialRecordBooks.Include(x => x.Contributions).ThenInclude(x => x.Contribution)
            .Include(x => x.ServiceCollections).ThenInclude(x => x.ServiceCollection)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Financial Record Book not found.");
        await DemandAccessAsync(frb, ct);
        if (frb.Status is not FinancialRecordBookStatuses.Draft and not FinancialRecordBookStatuses.Returned)
            throw new InvalidOperationException("Only a Draft or Returned FRB can be archived.");

        var previousStatus = frb.Status;
        if (frb.Contributions.Count > 0 || frb.ServiceCollections.Count > 0)
        {
            await SnapshotContributionsAsync(frb, ct);
            await SnapshotServiceCollectionsAsync(frb, ct);
            var calculations = await CalculateAsync(frb, useSnapshots: false, ct);
            db.FrbAssessmentSnapshots.RemoveRange(await db.FrbAssessmentSnapshots.Where(x => x.FinancialRecordBookId == id).ToListAsync(ct));
            db.FrbAssessmentSnapshots.AddRange(calculations.Assessments.Select(x => x.Snapshot));
            ApplyTotals(frb, calculations);
        }
        frb.Status = FinancialRecordBookStatuses.Archived;
        frb.UpdatedById = actorId;
        frb.UpdatedDate = DateTime.UtcNow;
        frb.Version++;
        AddAudit(id, "ARCHIVED", new { reason, previousStatus }, actorId);
        await db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<object> VerifyAsync(Guid id, FrbVerificationRequest request, Guid actorId, CancellationToken ct)
    {
        var officer = request.OfficerType.Trim().ToUpperInvariant();
        if (!FrbOfficerTypes.Required.Contains(officer)) throw new InvalidOperationException("Select a required FRB officer type.");
        var frb = await db.FinancialRecordBooks.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Financial Record Book not found.");
        if (frb.Status != FinancialRecordBookStatuses.AwaitingVerification) throw new InvalidOperationException("This FRB is not awaiting verification.");
        if (frb.CreatedById == actorId) throw new InvalidOperationException("The FRB creator cannot verify their own record.");
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == actorId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");
        var capability = officer switch { FrbOfficerTypes.StewardshipConvener => WorkflowCapabilities.FrbVerifyStewardship, FrbOfficerTypes.Treasurer => WorkflowCapabilities.FrbVerifyTreasurer, _ => WorkflowCapabilities.FrbVerifyMinister };
        var areaCode = officer switch { FrbOfficerTypes.StewardshipConvener => "STEWARDSHIP", FrbOfficerTypes.Treasurer => "TREASURY", _ => "MINISTRY" };
        if (!await workflow.CanAsync(actorId, capability, new AuthorizationResource(SectionId: frb.SectionId, FunctionalAreaCode: areaCode), ct))
            throw new UnauthorizedAccessException($"Only a user assigned the {OfficerLabel(officer)} designation can perform this verification.");
        var assignedDesignation = await ActiveDesignationForCapabilityAsync(actorId, capability, ct);
        if (string.IsNullOrWhiteSpace(user.SignatureData)) throw new InvalidOperationException("Add your digital signature to your profile before verifying an FRB.");
        if (await db.FrbVerifications.AnyAsync(x => x.FinancialRecordBookId == id && x.OfficerType == officer, ct))
            throw new InvalidOperationException($"The {OfficerLabel(officer)} verification has already been recorded.");
        db.FrbVerifications.Add(new FrbVerification
        {
            Id = Guid.NewGuid(), FinancialRecordBookId = id, OfficerType = officer, VerifiedById = actorId,
            OfficerName = $"{user.FirstName} {user.LastName}".Trim(), OfficerRole = assignedDesignation == null ? user.Role : $"{user.Role} / {assignedDesignation.Name}",
            SignatureData = user.SignatureData, Comments = Clean(request.Comments), VerifiedAtUtc = DateTime.UtcNow,
            CreatedById = actorId, UpdatedById = actorId
        });
        AddAudit(id, "VERIFIED_" + officer, new { comments = Clean(request.Comments) }, actorId);
        await db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<object> FinalizeAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var frb = await db.FinancialRecordBooks.Include(x => x.Contributions).ThenInclude(x => x.Contribution)
            .Include(x => x.ServiceCollections).ThenInclude(x => x.ServiceCollection)
            .Include(x => x.AssessmentSnapshots).Include(x => x.Verifications)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Financial Record Book not found.");
        if (!await workflow.CanAsync(actorId, WorkflowCapabilities.FrbFinalize, new AuthorizationResource(SectionId: frb.SectionId, FunctionalAreaCode: "TREASURY"), ct))
            throw new UnauthorizedAccessException("Only a user with an effective Treasurer appointment in scope can finalise an FRB.");
        await DemandAccessAsync(frb, ct);
        if (frb.Status != FinancialRecordBookStatuses.AwaitingVerification) throw new InvalidOperationException("Only an FRB awaiting verification can be finalised.");
        var missing = FrbOfficerTypes.Required.Except(frb.Verifications.Select(x => x.OfficerType)).Select(OfficerLabel).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException($"FRB cannot be finalised. Outstanding verification: {string.Join(", ", missing)}.");
        var duplicate = frb.Contributions.FirstOrDefault(x => x.Contribution!.FinalizedFinancialRecordBookId.HasValue && x.Contribution.FinalizedFinancialRecordBookId != id);
        if (duplicate != null) throw new InvalidOperationException($"Contribution {duplicate.Contribution!.ContributionNumber} has already been finalised in another FRB.");
        var duplicateCollection = frb.ServiceCollections.FirstOrDefault(x => x.ServiceCollection!.FinalizedFinancialRecordBookId.HasValue && x.ServiceCollection.FinalizedFinancialRecordBookId != id);
        if (duplicateCollection != null) throw new InvalidOperationException($"Service Collection {duplicateCollection.ServiceCollection!.CollectionNumber} has already been finalised in another FRB.");
        var calculations = await CalculateAsync(frb, useSnapshots: true, ct);
        if (calculations.Gross != frb.GrossContributions || calculations.AssessmentTotal != frb.AssessmentTotal || calculations.Net != frb.NetPosition)
            throw new InvalidOperationException("Financial reconciliation failed. Re-submit the FRB so its assessment snapshot can be refreshed.");
        foreach (var link in frb.Contributions) link.Contribution!.FinalizedFinancialRecordBookId = id;
        var serviceCollectionIds = await db.FinancialRecordBookServiceCollections.Where(x => x.FinancialRecordBookId == id).Select(x => x.ServiceCollectionId).ToListAsync(ct);
        foreach (var collection in await db.ServiceCollections.Where(x => serviceCollectionIds.Contains(x.Id)).ToListAsync(ct)) collection.FinalizedFinancialRecordBookId = id;
        frb.Status = FinancialRecordBookStatuses.Finalized; frb.FinalizedAtUtc = DateTime.UtcNow;
        frb.FinalizedById = actorId; frb.UpdatedById = actorId; frb.UpdatedDate = DateTime.UtcNow; frb.Version++;
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (DbUpdateException ex) { await tx.RollbackAsync(ct); throw new InvalidOperationException("Finalisation failed because one or more Contributions were finalised in another FRB.", ex); }
        return (await GetAsync(id, ct))!;
    }

    private async Task<IQueryable<FinancialRecordBook>> ScopedAsync(IQueryable<FinancialRecordBook> query, CancellationToken ct)
    {
        if (groupAccess.HasGlobalAccess() || await HasFrbOrganizationAccessAsync(ct)) return query;
        var sectionIds = await groupAccess.GetAccessibleSectionIdsAsync();
        return query.Where(x => x.SectionId.HasValue && sectionIds.Contains(x.SectionId.Value));
    }

    private static IQueryable<FinancialRecordBook> Full(IQueryable<FinancialRecordBook> query) => query
        .Include(x => x.Section).Include(x => x.Contributions).ThenInclude(x => x.Contribution!).ThenInclude(x => x.ContributionType)
        .Include(x => x.ServiceCollections).ThenInclude(x => x.ServiceCollection)
        .Include(x => x.AssessmentSnapshots).ThenInclude(x => x.AllocationLines).Include(x => x.Verifications);

    private async Task<object> PresentAsync(FinancialRecordBook frb, CancellationToken ct)
    {
        var createdBy = await db.Users.AsNoTracking().Where(x => x.Id == frb.CreatedById).Select(x => x.FirstName + " " + x.LastName).FirstOrDefaultAsync(ct);
        var frozen = frb.Status is not FinancialRecordBookStatuses.Draft and not FinancialRecordBookStatuses.Returned;
        var details = frb.Contributions.Select(x => new { x.ContributionId,
            ContributionNumber = frozen ? x.ContributionNumberSnapshot! : x.Contribution!.ContributionNumber,
            ContributionDate = frozen ? x.ContributionDateSnapshot!.Value.ToString("yyyy-MM-dd") : x.Contribution!.ContributionDate.ToString("O"),
            ContributionTypeId = frozen ? x.ContributionTypeIdSnapshot!.Value : x.Contribution!.ContributionTypeId,
            ContributionType = frozen ? x.ContributionTypeNameSnapshot! : x.Contribution!.ContributionType!.Name,
            Description = frozen ? x.DescriptionSnapshot : x.Contribution!.Description,
            Amount = frozen ? x.AmountSnapshot!.Value : x.Contribution!.Amount,
            Status = frozen ? x.StatusSnapshot! : x.Contribution!.Status }).ToList();
        List<FrbRevenueLineSnapshot> revenueLines = [];
        Dictionary<Guid, List<FrbRevenueLineSnapshot>> serviceLines;
        if (frozen)
        {
            foreach (var link in frb.Contributions)
            {
                var lines = DeserializeLines(link.LinesSnapshotJson);
                if (lines.Count > 0) revenueLines.AddRange(lines);
                else if (link.ContributionTypeIdSnapshot.HasValue && link.AmountSnapshot.HasValue)
                    revenueLines.Add(new FrbRevenueLineSnapshot(link.ContributionId, link.ContributionTypeIdSnapshot.Value,
                        link.ContributionTypeNameSnapshot ?? "Contribution", link.AmountSnapshot.Value, null, null,
                        "CONTRIBUTION", link.ContributionNumberSnapshot ?? "", link.ContributionDateSnapshot?.ToDateTime(TimeOnly.MinValue) ?? frb.CreatedDate));
            }
            serviceLines = frb.ServiceCollections.ToDictionary(
                x => x.ServiceCollectionId, x => DeserializeLines(x.LinesSnapshotJson));
        }
        else
        {
            var contributionIds = frb.Contributions.Select(x => x.ContributionId).ToArray();
            var liveContributions = await db.Contributions.AsNoTracking().Include(x => x.ContributionType)
                .Include(x => x.Allocations).ThenInclude(x => x.ContributionType)
                .Where(x => contributionIds.Contains(x.Id) && !x.IsArchived && x.ReversesContributionId == null).ToListAsync(ct);
            revenueLines.AddRange(liveContributions.SelectMany(ContributionLines));
            var collectionIds = frb.ServiceCollections.Select(x => x.ServiceCollectionId).ToArray();
            var liveCollections = await db.ServiceCollections.AsNoTracking().Include(x => x.ServiceEvent)
                .Include(x => x.Entries).ThenInclude(x => x.Lines).ThenInclude(x => x.ContributionType)
                .Where(x => collectionIds.Contains(x.Id)).ToListAsync(ct);
            serviceLines = liveCollections.ToDictionary(x => x.Id, ServiceCollectionLines);
        }
        var summaries = revenueLines.GroupBy(x => new { x.ContributionTypeId, x.ContributionType }).Select(g => new
        { g.Key.ContributionTypeId, g.Key.ContributionType, Entries = g.Count(), Amount = Money(g.Sum(x => x.Amount)), Contributions = g.ToList() }).OrderBy(x => x.ContributionType).ToList();
        var calculations = await CalculateAsync(frb, frb.Status != FinancialRecordBookStatuses.Draft && frb.Status != FinancialRecordBookStatuses.Returned, ct);
        return new { frb.Id, frb.FrbNumber, frb.PeriodStart, frb.PeriodEnd, frb.SectionId, Section = frb.Section?.SectionName,
            frb.Status, frb.Notes, frb.ReviewerComments, GrossContributions = calculations.Gross, AssessmentTotal = calculations.AssessmentTotal,
            NetPosition = calculations.Net, frb.CreatedById, CreatedBy = createdBy, frb.CreatedDate, frb.SubmittedAtUtc, frb.FinalizedAtUtc,
            AssessmentRuleIds = frb.AssessmentRuleIdsJson == null
                ? calculations.Assessments.Select(x => x.Snapshot.SourceRuleId).ToArray()
                : JsonSerializer.Deserialize<Guid[]>(frb.AssessmentRuleIdsJson) ?? [],
            ServiceCollections = frb.ServiceCollections.Select(x => new { x.ServiceCollectionId, x.CollectionNumberSnapshot, x.PurposeTotalSnapshot,
                x.ServiceDateSnapshot, x.StatusSnapshot,
                Lines = serviceLines.TryGetValue(x.ServiceCollectionId, out var lines) ? lines : [] }),
            Contributions = details, RevenueLines = revenueLines, Summaries = summaries, Assessments = calculations.Assessments.Select(x => new { x.Snapshot.SourceRuleId,
                x.Snapshot.RuleName, x.Snapshot.Classification, x.Snapshot.CalculationMethod,
                x.Snapshot.AssessmentBase, x.Snapshot.CalculatedAmount,
                AllocationLines = SnapshotLines(x.Snapshot).OrderBy(l => l.DisplayOrder).Select(l => new
                    { l.Beneficiary, l.Rate, l.FixedAmount, l.CalculatedAmount, l.IsLocalRetained, l.DisplayOrder, l.Notes }),
                ContributionTypeIds = JsonSerializer.Deserialize<Guid[]>(x.Snapshot.ContributionTypeIdsJson) ?? [],
                ContributionTypeNames = JsonSerializer.Deserialize<string[]>(x.Snapshot.ContributionTypeNamesJson) ?? [] }),
            Verifications = FrbOfficerTypes.Required.Select(type => { var v = frb.Verifications.FirstOrDefault(x => x.OfficerType == type); return new
                { OfficerType = type, Officer = OfficerLabel(type), Status = v == null ? "PENDING" : "SIGNED", v?.OfficerName, v?.OfficerRole, v?.SignatureData, v?.Comments, v?.VerifiedAtUtc }; }),
            IsFinalized = frb.Status == FinancialRecordBookStatuses.Finalized, DocumentStatus = frb.Status == FinancialRecordBookStatuses.Finalized ? "FINAL" : "NOT FULLY VERIFIED" };
    }

    private async Task ReplaceContributionsAsync(FinancialRecordBook frb, IEnumerable<Guid> ids, Guid actorId, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToArray();
        await EnsureEligibleAsync(frb, distinct, ct);
        var removed = frb.Contributions.Where(x => !distinct.Contains(x.ContributionId)).ToList();
        db.FinancialRecordBookContributions.RemoveRange(removed);
        var existing = frb.Contributions.Select(x => x.ContributionId).ToHashSet();
        foreach (var id in distinct.Where(x => !existing.Contains(x)))
        {
            var link = new FinancialRecordBookContribution
                { Id = Guid.NewGuid(), FinancialRecordBookId = frb.Id, ContributionId = id, CreatedById = actorId, UpdatedById = actorId };
            frb.Contributions.Add(link);
            db.FinancialRecordBookContributions.Add(link);
        }
        AddAudit(frb.Id, "CONTRIBUTIONS_SELECTED", new { contributionIds = distinct }, actorId);
    }

    private async Task ReplaceServiceCollectionsAsync(FinancialRecordBook frb, IEnumerable<Guid> ids, Guid actorId, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToArray();
        await EnsureServiceCollectionsEligibleAsync(frb, distinct, ct);
        var removed = frb.ServiceCollections.Where(x => !distinct.Contains(x.ServiceCollectionId)).ToList();
        db.FinancialRecordBookServiceCollections.RemoveRange(removed);
        var existing = frb.ServiceCollections.Select(x => x.ServiceCollectionId).ToHashSet();
        var collections = await db.ServiceCollections.AsNoTracking().Where(x => distinct.Contains(x.Id)).ToListAsync(ct);
        foreach (var collection in collections.Where(x => !existing.Contains(x.Id)))
        {
            var link = new FinancialRecordBookServiceCollection { Id = Guid.NewGuid(), FinancialRecordBookId = frb.Id,
                ServiceCollectionId = collection.Id, CollectionNumberSnapshot = collection.CollectionNumber,
                PurposeTotalSnapshot = collection.PurposeTotal, StatusSnapshot = collection.Status,
                CreatedById = actorId, UpdatedById = actorId };
            frb.ServiceCollections.Add(link); db.FinancialRecordBookServiceCollections.Add(link);
        }
        var serviceContributionIds = await ServiceCollectionContributionIdsAsync(frb, distinct, ct);
        var existingContributionIds = frb.Contributions.Select(x => x.ContributionId).ToHashSet();
        foreach (var contributionId in serviceContributionIds.Where(x => !existingContributionIds.Contains(x)))
        {
            var contributionLink = new FinancialRecordBookContribution
            {
                Id = Guid.NewGuid(), FinancialRecordBookId = frb.Id, ContributionId = contributionId,
                CreatedById = actorId, UpdatedById = actorId
            };
            frb.Contributions.Add(contributionLink);
            db.FinancialRecordBookContributions.Add(contributionLink);
        }
        AddAudit(frb.Id, "SERVICE_COLLECTIONS_SELECTED", new { serviceCollectionIds = distinct }, actorId);
    }

    private async Task EnsureEligibleAsync(FinancialRecordBook frb, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var requested = ids.Distinct().ToArray(); if (requested.Length == 0) return;
        var start = frb.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = frb.PeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var eligible = db.Contributions.Where(x => requested.Contains(x.Id) && !x.IsArchived && x.ReversesContributionId == null &&
            (x.Status == "CONFIRMED" || x.Status == "ALLOCATED") && x.ContributionDate >= start && x.ContributionDate < end &&
            (!x.FinalizedFinancialRecordBookId.HasValue || x.FinalizedFinancialRecordBookId == frb.Id));
        if (frb.SectionId.HasValue) eligible = eligible.Where(x => x.Allocations.Any(a => a.SectionId == frb.SectionId));
        var found = await eligible.Select(x => x.Id).ToListAsync(ct);
        if (found.Count != requested.Length) throw new InvalidOperationException("One or more selected Contributions are outside the period, organization, approved statuses, or are already in a finalised FRB.");
    }

    private async Task EnsureServiceCollectionsEligibleAsync(FinancialRecordBook frb, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var requested = ids.Distinct().ToArray(); if (requested.Length == 0) return;
        var start = frb.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = frb.PeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var eligible = db.ServiceCollections.Where(x => requested.Contains(x.Id) && x.Status == ServiceCollectionStatuses.Approved &&
            x.ServiceEvent!.StartsAtUtc >= start && x.ServiceEvent.StartsAtUtc < end &&
            (!x.FinalizedFinancialRecordBookId.HasValue || x.FinalizedFinancialRecordBookId == frb.Id));
        if (frb.SectionId.HasValue) eligible = eligible.Where(x => x.SectionId == frb.SectionId);
        var found = await eligible.Select(x => x.Id).ToListAsync(ct);
        if (found.Count != requested.Length) throw new InvalidOperationException("One or more selected Service Collections are outside the period or scope, not approved, or already finalised in another FRB.");
    }

    private async Task<Guid[]> ServiceCollectionContributionIdsAsync(
        FinancialRecordBook frb, IEnumerable<Guid> serviceCollectionIds, CancellationToken ct)
    {
        var collectionIds = serviceCollectionIds.Distinct().ToArray();
        if (collectionIds.Length == 0) return [];
        var lines = await db.ServiceCollectionEntryLines.AsNoTracking()
            .Where(x => collectionIds.Contains(x.ServiceCollectionEntry!.ServiceCollectionId))
            .Select(x => new { x.Id, x.ContributionId })
            .ToListAsync(ct);
        if (lines.Count == 0 || lines.Any(x => !x.ContributionId.HasValue))
            throw new InvalidOperationException("Every selected Service Collection must be fully posted to the central Contributions register before it can be included in an FRB.");
        var contributionIds = lines.Select(x => x.ContributionId!.Value).Distinct().ToArray();
        var start = frb.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = frb.PeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var eligible = db.Contributions.AsNoTracking().Where(x => contributionIds.Contains(x.Id) && !x.IsArchived &&
            x.ReversesContributionId == null && (x.Status == "CONFIRMED" || x.Status == "ALLOCATED") &&
            x.ContributionDate >= start && x.ContributionDate < end &&
            (!x.FinalizedFinancialRecordBookId.HasValue || x.FinalizedFinancialRecordBookId == frb.Id));
        if (frb.SectionId.HasValue)
            eligible = eligible.Where(x => x.Allocations.Any(a => a.SectionId == frb.SectionId));
        if (await eligible.CountAsync(ct) != contributionIds.Length)
            throw new InvalidOperationException("One or more Contributions belonging to a selected Service Collection are invalid, outside scope, or already finalised in another FRB.");
        return contributionIds;
    }

    private async Task RecalculateDraftAsync(FinancialRecordBook frb, CancellationToken ct) => ApplyTotals(frb, await CalculateAsync(frb, false, ct));

    private async Task SnapshotContributionsAsync(FinancialRecordBook frb, CancellationToken ct)
    {
        var ids = frb.Contributions.Select(x => x.ContributionId).ToArray();
        var source = await db.Contributions.AsNoTracking().Include(x => x.ContributionType)
            .Include(x => x.Allocations).ThenInclude(x => x.ContributionType)
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var link in frb.Contributions)
        {
            var contribution = source[link.ContributionId];
            link.ContributionNumberSnapshot = contribution.ContributionNumber;
            link.ContributionDateSnapshot = DateOnly.FromDateTime(contribution.ContributionDate);
            link.ContributionTypeIdSnapshot = contribution.ContributionTypeId;
            link.ContributionTypeNameSnapshot = contribution.ContributionType!.Name;
            link.DescriptionSnapshot = contribution.Description;
            link.AmountSnapshot = Money(contribution.Amount);
            link.StatusSnapshot = contribution.Status;
            link.LinesSnapshotJson = JsonSerializer.Serialize(ContributionLines(contribution));
            link.UpdatedDate = DateTime.UtcNow;
        }
    }

    private async Task SnapshotServiceCollectionsAsync(FinancialRecordBook frb, CancellationToken ct)
    {
        var ids = frb.ServiceCollections.Select(x => x.ServiceCollectionId).ToArray();
        var source = await db.ServiceCollections.AsNoTracking().Include(x => x.ServiceEvent)
            .Include(x => x.Entries).ThenInclude(x => x.Lines).ThenInclude(x => x.ContributionType)
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        foreach (var link in frb.ServiceCollections)
        {
            var collection = source[link.ServiceCollectionId];
            link.CollectionNumberSnapshot = collection.CollectionNumber;
            link.PurposeTotalSnapshot = Money(collection.PurposeTotal);
            link.ServiceDateSnapshot = collection.ServiceEvent!.StartsAtUtc;
            link.StatusSnapshot = collection.Status;
            link.LinesSnapshotJson = JsonSerializer.Serialize(ServiceCollectionLines(collection));
            link.UpdatedDate = DateTime.UtcNow;
        }
    }

    private async Task<Calculation> CalculateAsync(FinancialRecordBook frb, bool useSnapshots, CancellationToken ct)
    {
        List<(Guid TypeId, decimal Amount)> values = [];
        if (useSnapshots)
        {
            foreach (var link in frb.Contributions)
            {
                var lines = DeserializeLines(link.LinesSnapshotJson);
                if (lines.Count > 0) values.AddRange(lines.Select(x => (x.ContributionTypeId, x.Amount)));
                else if (link.ContributionTypeIdSnapshot.HasValue && link.AmountSnapshot.HasValue)
                    values.Add((link.ContributionTypeIdSnapshot.Value, link.AmountSnapshot.Value));
            }
        }
        else
        {
            var ids = frb.Contributions.Select(x => x.ContributionId).ToArray();
            var live = await db.Contributions.AsNoTracking().Include(x => x.ContributionType)
                .Include(x => x.Allocations).ThenInclude(x => x.ContributionType)
                .Where(x => ids.Contains(x.Id) && !x.IsArchived && x.ReversesContributionId == null).ToListAsync(ct);
            values.AddRange(live.SelectMany(ContributionLines).Select(x => (x.ContributionTypeId, x.Amount)));
        }
        var gross = Money(values.Sum(x => x.Amount));
        List<FrbAssessmentSnapshot> snapshots;
        if (useSnapshots)
            snapshots = frb.AssessmentSnapshots.Count > 0 ? frb.AssessmentSnapshots.ToList() : await db.FrbAssessmentSnapshots.AsNoTracking()
                .Include(x => x.AllocationLines).Where(x => x.FinancialRecordBookId == frb.Id).ToListAsync(ct);
        else
        {
            var effective = frb.PeriodEnd;
            var rules = await db.FrbAssessmentRules.AsNoTracking().Include(x => x.ContributionTypes).ThenInclude(x => x.ContributionType).Include(x => x.AllocationLines)
                .Where(x => x.IsActive && x.EffectiveFrom <= effective && (!x.EffectiveTo.HasValue || x.EffectiveTo >= effective)).ToListAsync(ct);
            rules = rules.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(rule => rule.EffectiveFrom).First()).ToList();
            var selectedTypes = values.Select(x => x.TypeId).ToHashSet();
            rules = rules.Where(rule => rule.ContributionTypes.Any(type => selectedTypes.Contains(type.ContributionTypeId))).ToList();
            if (frb.AssessmentRuleIdsJson != null)
            {
                var selectedRuleIds = (JsonSerializer.Deserialize<Guid[]>(frb.AssessmentRuleIdsJson) ?? []).ToHashSet();
                if (rules.Count == 0)
                    throw new InvalidOperationException("No active assessment is configured for the selected Contribution Types and FRB period.");
                var invalidRuleIds = selectedRuleIds.Except(rules.Select(x => x.Id)).ToArray();
                if (invalidRuleIds.Length > 0)
                    throw new InvalidOperationException("One or more selected assessments are inactive, outside the FRB period, or do not apply to the selected Contribution Types.");
                if (rules.Count > 0 && selectedRuleIds.Count == 0)
                    throw new InvalidOperationException("Select at least one applicable assessment for this Financial Record Book.");
                rules = rules.Where(x => selectedRuleIds.Contains(x.Id)).ToList();
            }
            snapshots = rules.Select(rule =>
            {
                var typeIds = rule.ContributionTypes.Select(x => x.ContributionTypeId).ToHashSet();
                var basis = Money(values.Where(x => typeIds.Contains(x.TypeId)).Sum(x => x.Amount));
                var snapshotId = Guid.NewGuid();
                var sourceLines = rule.AllocationLines.OrderBy(x => x.DisplayOrder).ToList();
                var allocationLines = sourceLines.Select((line, index) => new FrbAssessmentAllocationSnapshot
                {
                    Id = Guid.NewGuid(), FrbAssessmentSnapshotId = snapshotId, SourceAllocationLineId = line.Id == Guid.Empty ? null : line.Id,
                    Beneficiary = line.Beneficiary, Rate = line.Rate, FixedAmount = line.FixedAmount,
                    CalculatedAmount = FrbFinancialCalculator.Assessment(rule.CalculationMethod, basis, line.Rate, line.FixedAmount),
                    IsLocalRetained = line.IsLocalRetained, DisplayOrder = line.DisplayOrder == 0 ? index : line.DisplayOrder,
                    Notes = line.Notes, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
                }).ToList();
                return new FrbAssessmentSnapshot { Id = snapshotId, FinancialRecordBookId = frb.Id, SourceRuleId = rule.Id,
                    RuleName = rule.Name, Classification = rule.Classification, CalculationMethod = rule.CalculationMethod,
                    AssessmentBase = basis,
                    CalculatedAmount = Money(allocationLines.Sum(x => x.CalculatedAmount)), AllocationLines = allocationLines,
                    ContributionTypeIdsJson = JsonSerializer.Serialize(typeIds),
                    ContributionTypeNamesJson = JsonSerializer.Serialize(rule.ContributionTypes.Select(x => x.ContributionType!.Name).OrderBy(x => x)), CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
            }).Where(x => x.AssessmentBase > 0 || x.CalculatedAmount != 0).ToList();
        }
        var deductions = snapshots.Where(x => x.Classification == "DEDUCTION").Sum(x => x.CalculatedAmount);
        var additions = snapshots.Where(x => x.Classification == "ADDITION").Sum(x => x.CalculatedAmount);
        var externalAllocations = snapshots.Where(x => x.Classification == "ALLOCATION")
            .Sum(x => SnapshotLines(x).Where(line => !line.IsLocalRetained).Sum(line => line.CalculatedAmount));
        var assessmentTotal = snapshots.Sum(x => x.CalculatedAmount);
        return new Calculation(gross, Money(assessmentTotal), FrbFinancialCalculator.CongregationalPosition(gross, additions, deductions, externalAllocations), snapshots.Select(x => new Assessment(x)).ToList());
    }

    private static IReadOnlyCollection<FrbAssessmentAllocationSnapshot> SnapshotLines(FrbAssessmentSnapshot snapshot) =>
        snapshot.AllocationLines.ToList();

    private static List<FrbRevenueLineSnapshot> ContributionLines(Contribution contribution)
    {
        if (contribution.Allocations.Count > 0)
            return contribution.Allocations.Select(x => new FrbRevenueLineSnapshot(x.Id,
                x.ContributionTypeId ?? contribution.ContributionTypeId,
                x.ContributionType?.Name ?? contribution.ContributionType?.Name ?? "Contribution",
                Money(x.AllocatedAmount), contribution.MemberId, contribution.ContributorNameSnapshot,
                "CONTRIBUTION", contribution.ContributionNumber, contribution.ContributionDate)).ToList();
        return [new FrbRevenueLineSnapshot(contribution.Id, contribution.ContributionTypeId,
            contribution.ContributionType?.Name ?? "Contribution", Money(contribution.Amount), contribution.MemberId,
            contribution.ContributorNameSnapshot, "CONTRIBUTION", contribution.ContributionNumber, contribution.ContributionDate)];
    }

    private static List<FrbRevenueLineSnapshot> ServiceCollectionLines(eMilimo.Core.Entities.ServiceCollection collection) =>
        collection.Entries.SelectMany(entry => entry.Lines.Select(line => new FrbRevenueLineSnapshot(line.Id,
            line.ContributionTypeId, line.ContributionType?.Name ?? "Contribution", Money(line.Amount), entry.MemberId,
            entry.ContributorNameSnapshot, "SERVICE_COLLECTION", collection.CollectionNumber,
            collection.ServiceEvent!.StartsAtUtc))).ToList();

    private static List<FrbRevenueLineSnapshot> DeserializeLines(string? json) => string.IsNullOrWhiteSpace(json)
        ? [] : JsonSerializer.Deserialize<List<FrbRevenueLineSnapshot>>(json) ?? [];

    private static void ApplyTotals(FinancialRecordBook frb, Calculation value)
    { frb.GrossContributions = value.Gross; frb.AssessmentTotal = value.AssessmentTotal; frb.NetPosition = value.Net; }
    private async Task DemandAccessAsync(FinancialRecordBook frb, CancellationToken ct)
    {
        if (await HasFrbOrganizationAccessAsync(ct)) return;
        if (frb.SectionId.HasValue) await DemandSectionAccessAsync(frb.SectionId.Value, ct);
        else if (!groupAccess.HasGlobalAccess()) throw new UnauthorizedAccessException("Only users with organization-wide access can use an organization-wide FRB.");
    }
    private async Task<bool> HasFrbOrganizationAccessAsync(CancellationToken ct)
    {
        var userId = currentUser.GetUserId();
        if (!userId.HasValue) return false;
        var now = DateTime.UtcNow;
        return await workflow.HasAsync(userId.Value, WorkflowCapabilities.FrbOrganizationScope, ct);
    }
    private async Task<Designation?> ActiveDesignationForCapabilityAsync(Guid userId, string capability, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.OfficeAppointments.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EffectiveFromUtc <= now && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) && x.Designation.IsActive && x.FunctionalArea.IsActive)
            .Select(x => x.Designation).FirstOrDefaultAsync(x => x.CapabilitiesJson.Contains(capability), ct);
    }
    private async Task DemandSectionAccessAsync(Guid sectionId, CancellationToken ct)
    {
        if (!await db.Sections.AnyAsync(x => x.Id == sectionId && x.IsActive, ct)) throw new InvalidOperationException("Select an active Section.");
        if (!groupAccess.HasGlobalAccess() && !await groupAccess.CanAccessEntityAsync(null, sectionId, null)) throw new UnauthorizedAccessException("You are not assigned to this Section.");
    }
    private void AddAudit(Guid id, string action, object changes, Guid actor) => db.FrbAudits.Add(new FrbAud
        { Id = Guid.NewGuid(), FinancialRecordBookId = id, Action = action, ChangesJson = JsonSerializer.Serialize(changes), PerformedById = actor, PerformedAtUtc = DateTime.UtcNow });
    private static void ValidatePeriod(DateTime from, DateTime to)
    { if (from.Date > to.Date) throw new InvalidOperationException("Reporting period start must be on or before its end date."); if ((to.Date - from.Date).TotalDays > 366) throw new InvalidOperationException("The reporting period cannot exceed 366 days."); }
    private static void ValidateNotes(string? notes)
    { if (notes?.Length > 2000) throw new InvalidOperationException("FRB Notes / Remarks cannot exceed 2,000 characters."); }
    private static string? SerializeRuleSelection(IReadOnlyCollection<Guid>? ruleIds) =>
        ruleIds == null ? null : JsonSerializer.Serialize(ruleIds.Distinct().OrderBy(x => x));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string OfficerLabel(string type) => type switch { FrbOfficerTypes.StewardshipConvener => "Stewardship Convener", FrbOfficerTypes.Treasurer => "Treasurer", _ => "Minister" };
    private sealed record Assessment(FrbAssessmentSnapshot Snapshot);
    private sealed record Calculation(decimal Gross, decimal AssessmentTotal, decimal Net, List<Assessment> Assessments);
    private sealed record FrbRevenueLineSnapshot(Guid SourceLineId, Guid ContributionTypeId, string ContributionType,
        decimal Amount, Guid? MemberId, string? ContributorName, string SourceModule, string SourceReference, DateTime TransactionDate);
}
