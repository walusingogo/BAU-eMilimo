using eMilimo.Core.Entities;

namespace eMilimo.Core.Services;

public sealed record OrganizationFinancialBreakdown(
    decimal OrganizationWideTotal,
    decimal SectionTotal,
    decimal GroupTotal,
    decimal CommitteeTotal,
    decimal GrandTotal);

public sealed record OrganizationOverviewDashboard(
    string OrganizationName,
    OrganizationFinancialBreakdown Breakdown,
    int ActiveSectionCount,
    int ActiveGroupCount,
    int ActiveCommitteeCount,
    DateTime? LastUpdatedUtc,
    IReadOnlyList<ContributionTypeBreakdown> ContributionTypeBreakdowns);

public sealed record ContributionTypeBreakdown(
    Guid ContributionTypeId,
    string Code,
    string Name,
    decimal TotalAmount,
    int ContributionCount);

public sealed record SectionFinancialView(
    Guid SectionId,
    string SectionName,
    decimal DirectContributions,
    decimal GroupContributions,
    decimal CommitteeContributions,
    decimal TotalContributions,
    int ActiveGroupCount,
    int ActiveCommitteeCount);

public sealed record GroupFinancialView(
    Guid GroupId,
    string GroupName,
    Guid? SectionId,
    string? SectionName,
    decimal DirectContributions,
    decimal CommitteeContributions,
    decimal TotalContributions,
    int ActiveCommitteeCount);

public sealed record CommitteeFinancialView(
    Guid CommitteeId,
    string CommitteeName,
    Guid? ParentSectionId,
    Guid? ParentGroupId,
    string? ParentName,
    decimal TotalContributions,
    int ContributionCount);

public interface IOrganizationOverviewService
{
    /// <summary>
    /// Retrieves the organization-level financial overview.
    /// </summary>
    Task<OrganizationOverviewDashboard> GetOrganizationOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves organization-wide (not allocated to any structure) financial summary.
    /// </summary>
    Task<(decimal Total, IReadOnlyList<ContributionTypeBreakdown> ByType)> GetOrganizationWideFinancialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves financial summary for a specific section with drill-down capability.
    /// </summary>
    Task<SectionFinancialView?> GetSectionFinancialViewAsync(Guid sectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all sections with their financial summaries.
    /// </summary>
    Task<IReadOnlyList<SectionFinancialView>> GetAllSectionsFinancialViewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves financial summary for a specific group with drill-down capability.
    /// </summary>
    Task<GroupFinancialView?> GetGroupFinancialViewAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all groups with their financial summaries.
    /// </summary>
    Task<IReadOnlyList<GroupFinancialView>> GetAllGroupsFinancialViewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves financial summary for a specific committee with drill-down capability.
    /// </summary>
    Task<CommitteeFinancialView?> GetCommitteeFinancialViewAsync(Guid committeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all committees with their financial summaries.
    /// </summary>
    Task<IReadOnlyList<CommitteeFinancialView>> GetAllCommitteesFinancialViewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the organization overview calculations.
    /// Should be called after contributions are posted or reversed.
    /// </summary>
    Task RefreshOrganizationOverviewAsync(Guid? actorUserId, CancellationToken cancellationToken = default);
}
