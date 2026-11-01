using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class OrganizationOverviewService : IOrganizationOverviewService
{
    private readonly AppDbContext _db;

    public OrganizationOverviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrganizationOverviewDashboard> GetOrganizationOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var overview = await _db.OrganizationOverviews.FirstOrDefaultAsync(cancellationToken);
        if (overview is null || overview.IsStale)
        {
            await RefreshOrganizationOverviewAsync(null, cancellationToken);
            overview = await _db.OrganizationOverviews.FirstOrDefaultAsync(cancellationToken);
        }

        if (overview is null)
            throw new InvalidOperationException("Organization overview could not be calculated.");

        var breakdowns = await _db.OrganizationContributionTypeSummaries
            .AsNoTracking()
            .Where(x => x.OrganizationOverviewId == overview.Id)
            .Include(x => x.ContributionType)
            .OrderBy(x => x.ContributionType!.Code)
            .Select(x => new ContributionTypeBreakdown(
                x.ContributionTypeId,
                x.ContributionType!.Code,
                x.ContributionType.Name,
                x.TotalAmount,
                x.ContributionCount))
            .ToListAsync(cancellationToken);

        return new OrganizationOverviewDashboard(
            overview.OrganizationName,
            new OrganizationFinancialBreakdown(
                overview.TotalOrganizationWideContributions,
                overview.TotalSectionContributions,
                overview.TotalGroupContributions,
                overview.TotalCommitteeContributions,
                overview.TotalContributions),
            overview.ActiveSectionCount,
            overview.ActiveGroupCount,
            overview.ActiveCommitteeCount,
            overview.LastUpdatedUtc,
            breakdowns);
    }

    public async Task<(decimal Total, IReadOnlyList<ContributionTypeBreakdown> ByType)> GetOrganizationWideFinancialsAsync(
        CancellationToken cancellationToken = default)
    {
        var organizationWideAllocations = await _db.ContributionAllocations
            .AsNoTracking()
            .Where(x => x.GroupId == null && x.SectionId == null && x.CommitteeId == null)
            .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                        (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
            .Include(x => x.ContributionType)
            .ToListAsync(cancellationToken);

        var total = organizationWideAllocations.Sum(x => x.AllocatedAmount);
            var byType = organizationWideAllocations
            .GroupBy(x => new { x.ContributionType!.Code, x.ContributionType.Name, x.ContributionTypeId })
            .Where(g => g.Key.ContributionTypeId.HasValue)
            .Select(g => new ContributionTypeBreakdown(
                g.Key.ContributionTypeId!.Value,
                g.Key.Code,
                g.Key.Name,
                g.Sum(x => x.AllocatedAmount),
                g.Count()))
            .OrderBy(x => x.Code)
            .ToList();

        return (total, byType);
    }

    public async Task<SectionFinancialView?> GetSectionFinancialViewAsync(
        Guid sectionId,
        CancellationToken cancellationToken = default)
    {
        var section = await _db.Sections.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sectionId, cancellationToken);
        if (section is null)
            return null;

        var summary = await _db.SectionFinancialSummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SectionId == sectionId, cancellationToken);

        return new SectionFinancialView(
            sectionId,
            section.SectionName,
            summary?.DirectContributions ?? 0,
            summary?.GroupContributions ?? 0,
            summary?.CommitteeContributions ?? 0,
            summary?.TotalContributions ?? 0,
            summary?.ActiveGroupCount ?? 0,
            summary?.ActiveCommitteeCount ?? 0);
    }

    public async Task<IReadOnlyList<SectionFinancialView>> GetAllSectionsFinancialViewAsync(
        CancellationToken cancellationToken = default)
    {
        var sections = await _db.Sections.AsNoTracking().ToListAsync(cancellationToken);
        var summaries = await _db.SectionFinancialSummaries.AsNoTracking().ToListAsync(cancellationToken);

        return sections
            .Select(s =>
            {
                var summary = summaries.FirstOrDefault(x => x.SectionId == s.Id);
                return new SectionFinancialView(
                    s.Id,
                    s.SectionName,
                    summary?.DirectContributions ?? 0,
                    summary?.GroupContributions ?? 0,
                    summary?.CommitteeContributions ?? 0,
                    summary?.TotalContributions ?? 0,
                    summary?.ActiveGroupCount ?? 0,
                    summary?.ActiveCommitteeCount ?? 0);
            })
            .OrderBy(x => x.SectionName)
            .ToList();
    }

    public async Task<GroupFinancialView?> GetGroupFinancialViewAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var group = await _db.Groups.AsNoTracking()
            .Include(x => x.Section)
            .FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group is null)
            return null;

        var summary = await _db.GroupFinancialSummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GroupId == groupId, cancellationToken);

        return new GroupFinancialView(
            groupId,
            group.GroupName,
            group.SectionId,
            group.Section?.SectionName,
            summary?.DirectContributions ?? 0,
            summary?.CommitteeContributions ?? 0,
            summary?.TotalContributions ?? 0,
            summary?.ActiveCommitteeCount ?? 0);
    }

    public async Task<IReadOnlyList<GroupFinancialView>> GetAllGroupsFinancialViewAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = await _db.Groups.AsNoTracking()
            .Include(x => x.Section)
            .ToListAsync(cancellationToken);
        var summaries = await _db.GroupFinancialSummaries.AsNoTracking().ToListAsync(cancellationToken);

        return groups
            .Select(g =>
            {
                var summary = summaries.FirstOrDefault(x => x.GroupId == g.Id);
                return new GroupFinancialView(
                    g.Id,
                    g.GroupName,
                    g.SectionId,
                    g.Section?.SectionName,
                    summary?.DirectContributions ?? 0,
                    summary?.CommitteeContributions ?? 0,
                    summary?.TotalContributions ?? 0,
                    summary?.ActiveCommitteeCount ?? 0);
            })
            .OrderBy(x => x.SectionName ?? "")
            .ThenBy(x => x.GroupName)
            .ToList();
    }

    public async Task<CommitteeFinancialView?> GetCommitteeFinancialViewAsync(
        Guid committeeId,
        CancellationToken cancellationToken = default)
    {
        var committee = await _db.Committees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == committeeId, cancellationToken);
        if (committee is null)
            return null;

        var summary = await _db.CommitteeFinancialSummaries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CommitteeId == committeeId, cancellationToken);

        string? parentName = null;
        if (committee.ParentSectionId.HasValue)
        {
            var section = await _db.Sections.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == committee.ParentSectionId, cancellationToken);
            parentName = section?.SectionName;
        }
        else if (committee.ParentGroupId.HasValue)
        {
            var group = await _db.Groups.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == committee.ParentGroupId, cancellationToken);
            parentName = group?.GroupName;
        }

        return new CommitteeFinancialView(
            committeeId,
            committee.CommitteeName,
            committee.ParentSectionId,
            committee.ParentGroupId,
            parentName,
            summary?.TotalContributions ?? 0,
            summary?.ContributionCount ?? 0);
    }

    public async Task<IReadOnlyList<CommitteeFinancialView>> GetAllCommitteesFinancialViewAsync(
        CancellationToken cancellationToken = default)
    {
        var committees = await _db.Committees.AsNoTracking().ToListAsync(cancellationToken);
        var summaries = await _db.CommitteeFinancialSummaries.AsNoTracking().ToListAsync(cancellationToken);
        var sections = await _db.Sections.AsNoTracking().ToListAsync(cancellationToken);
        var groups = await _db.Groups.AsNoTracking().ToListAsync(cancellationToken);

        return committees
            .Select(c =>
            {
                var summary = summaries.FirstOrDefault(x => x.CommitteeId == c.Id);
                string? parentName = null;
                if (c.ParentSectionId.HasValue)
                    parentName = sections.FirstOrDefault(x => x.Id == c.ParentSectionId)?.SectionName;
                else if (c.ParentGroupId.HasValue)
                    parentName = groups.FirstOrDefault(x => x.Id == c.ParentGroupId)?.GroupName;

                return new CommitteeFinancialView(
                    c.Id,
                    c.CommitteeName,
                    c.ParentSectionId,
                    c.ParentGroupId,
                    parentName,
                    summary?.TotalContributions ?? 0,
                    summary?.ContributionCount ?? 0);
            })
            .OrderBy(x => x.ParentName ?? "")
            .ThenBy(x => x.CommitteeName)
            .ToList();
    }

    public async Task RefreshOrganizationOverviewAsync(
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            // Get or create organization overview
            var overview = await _db.OrganizationOverviews.FirstOrDefaultAsync(cancellationToken);
            if (overview is null)
            {
                overview = new OrganizationOverview
                {
                    Id = Guid.NewGuid(),
                    OrganizationName = "Church",
                    CreatedDate = now,
                    UpdatedDate = now,
                    Version = 1,
                    CreatedById = actorUserId
                };
                _db.OrganizationOverviews.Add(overview);
            }

            // Calculate organization-wide contributions (all destinations null)
            var orgWideAllocations = await _db.ContributionAllocations
                .Where(x => x.GroupId == null && x.SectionId == null && x.CommitteeId == null)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .ToListAsync(cancellationToken);
            var orgWideTotal = orgWideAllocations.Sum(x => x.AllocatedAmount);

            // Calculate section contributions (direct only, not including groups/committees)
            var sectionAllocations = await _db.ContributionAllocations
                .Where(x => x.SectionId != null && x.GroupId == null && x.CommitteeId == null)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .ToListAsync(cancellationToken);
            var sectionTotal = sectionAllocations.Sum(x => x.AllocatedAmount);

            // Calculate group contributions (direct only, not including committees)
            var groupAllocations = await _db.ContributionAllocations
                .Where(x => x.GroupId != null && x.CommitteeId == null)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .ToListAsync(cancellationToken);
            var groupTotal = groupAllocations.Sum(x => x.AllocatedAmount);

            // Calculate committee contributions
            var committeeAllocations = await _db.ContributionAllocations
                .Where(x => x.CommitteeId != null)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .ToListAsync(cancellationToken);
            var committeeTotal = committeeAllocations.Sum(x => x.AllocatedAmount);

            overview.TotalOrganizationWideContributions = orgWideTotal;
            overview.TotalSectionContributions = sectionTotal;
            overview.TotalGroupContributions = groupTotal;
            overview.TotalCommitteeContributions = committeeTotal;
            overview.TotalContributions = orgWideTotal + sectionTotal + groupTotal + committeeTotal;
            overview.ActiveSectionCount = await _db.Sections.CountAsync(x => x.IsActive, cancellationToken);
            overview.ActiveGroupCount = await _db.Groups.CountAsync(x => x.IsActive, cancellationToken);
            overview.ActiveCommitteeCount = await _db.Committees.CountAsync(x => x.IsActive, cancellationToken);
            overview.LastUpdatedUtc = now;
            overview.IsStale = false;
            overview.UpdatedDate = now;
            overview.UpdatedById = actorUserId;
            overview.Version++;

            // Clear and rebuild contribution type summaries
            _db.OrganizationContributionTypeSummaries.RemoveRange(
                await _db.OrganizationContributionTypeSummaries
                    .Where(x => x.OrganizationOverviewId == overview.Id)
                    .ToListAsync(cancellationToken));

            var typeBreakdowns = orgWideAllocations
                .GroupBy(x => new { x.ContributionTypeId, x.ContributionType })
                .Select(g => new OrganizationContributionTypeSummary
                {
                    Id = Guid.NewGuid(),
                    OrganizationOverviewId = overview.Id,
                    ContributionTypeId = g.Key.ContributionTypeId ?? Guid.Empty,
                    TotalAmount = g.Sum(x => x.AllocatedAmount),
                    ContributionCount = g.Count(),
                    CreatedDate = now,
                    UpdatedDate = now,
                    Version = 1,
                    CreatedById = actorUserId
                })
                .ToList();
            _db.OrganizationContributionTypeSummaries.AddRange(typeBreakdowns);

            // Refresh section summaries
            await RefreshSectionSummariesAsync(cancellationToken);

            // Refresh group summaries
            await RefreshGroupSummariesAsync(cancellationToken);

            // Refresh committee summaries
            await RefreshCommitteeSummariesAsync(cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task RefreshSectionSummariesAsync(CancellationToken cancellationToken)
    {
        var sections = await _db.Sections.Where(x => x.IsActive).ToListAsync(cancellationToken);

        foreach (var section in sections)
        {
            var directAllocations = await _db.ContributionAllocations
                .Where(x => x.SectionId == section.Id && x.GroupId == null && x.CommitteeId == null)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .SumAsync(x => x.AllocatedAmount, cancellationToken);

            var groupAllocations = await _db.ContributionAllocations
                .Where(x => x.GroupId != null && x.CommitteeId == null)
                .Where(x => x.Group!.SectionId == section.Id)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .SumAsync(x => x.AllocatedAmount, cancellationToken);

            var committeeAllocations = await _db.ContributionAllocations
                .Where(x => x.CommitteeId != null)
                .Where(x => x.Committee!.ParentSectionId == section.Id)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .SumAsync(x => x.AllocatedAmount, cancellationToken);

            var activeGroupCount = await _db.Groups.CountAsync(
                x => x.SectionId == section.Id && x.IsActive, cancellationToken);
            var activeCommitteeCount = await _db.Committees.CountAsync(
                x => x.ParentSectionId == section.Id && x.IsActive, cancellationToken);

            var summary = await _db.SectionFinancialSummaries
                .FirstOrDefaultAsync(x => x.SectionId == section.Id, cancellationToken);

            if (summary is null)
            {
                summary = new SectionFinancialSummary
                {
                    Id = Guid.NewGuid(),
                    SectionId = section.Id,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                };
                _db.SectionFinancialSummaries.Add(summary);
            }

            summary.DirectContributions = directAllocations;
            summary.GroupContributions = groupAllocations;
            summary.CommitteeContributions = committeeAllocations;
            summary.TotalContributions = directAllocations + groupAllocations + committeeAllocations;
            summary.ActiveGroupCount = activeGroupCount;
            summary.ActiveCommitteeCount = activeCommitteeCount;
            summary.LastUpdatedUtc = DateTime.UtcNow;
            summary.UpdatedDate = DateTime.UtcNow;
            summary.Version++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshGroupSummariesAsync(CancellationToken cancellationToken)
    {
        var groups = await _db.Groups.Where(x => x.IsActive).ToListAsync(cancellationToken);

        foreach (var group in groups)
        {
            var directAllocations = await _db.ContributionAllocations
                .Where(x => x.GroupId == group.Id && x.CommitteeId == null)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .SumAsync(x => x.AllocatedAmount, cancellationToken);

            var committeeAllocations = await _db.ContributionAllocations
                .Where(x => x.CommitteeId != null)
                .Where(x => x.Committee!.ParentGroupId == group.Id)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .SumAsync(x => x.AllocatedAmount, cancellationToken);

            var activeCommitteeCount = await _db.Committees.CountAsync(
                x => x.ParentGroupId == group.Id && x.IsActive, cancellationToken);

            var summary = await _db.GroupFinancialSummaries
                .FirstOrDefaultAsync(x => x.GroupId == group.Id, cancellationToken);

            if (summary is null)
            {
                summary = new GroupFinancialSummary
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                };
                _db.GroupFinancialSummaries.Add(summary);
            }

            summary.DirectContributions = directAllocations;
            summary.CommitteeContributions = committeeAllocations;
            summary.TotalContributions = directAllocations + committeeAllocations;
            summary.ActiveCommitteeCount = activeCommitteeCount;
            summary.LastUpdatedUtc = DateTime.UtcNow;
            summary.UpdatedDate = DateTime.UtcNow;
            summary.Version++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshCommitteeSummariesAsync(CancellationToken cancellationToken)
    {
        var committees = await _db.Committees.Where(x => x.IsActive).ToListAsync(cancellationToken);

        foreach (var committee in committees)
        {
            var allocations = await _db.ContributionAllocations
                .Where(x => x.CommitteeId == committee.Id)
                .Where(x => !x.Contribution!.IsArchived && x.Contribution.ReversesContributionId == null &&
                            (x.Contribution.Status == "CONFIRMED" || x.Contribution.Status == "POSTED"))
                .ToListAsync(cancellationToken);

            var summary = await _db.CommitteeFinancialSummaries
                .FirstOrDefaultAsync(x => x.CommitteeId == committee.Id, cancellationToken);

            if (summary is null)
            {
                summary = new CommitteeFinancialSummary
                {
                    Id = Guid.NewGuid(),
                    CommitteeId = committee.Id,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                };
                _db.CommitteeFinancialSummaries.Add(summary);
            }

            summary.TotalContributions = allocations.Sum(x => x.AllocatedAmount);
            summary.ContributionCount = allocations.Count;
            summary.LastUpdatedUtc = DateTime.UtcNow;
            summary.UpdatedDate = DateTime.UtcNow;
            summary.Version++;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
