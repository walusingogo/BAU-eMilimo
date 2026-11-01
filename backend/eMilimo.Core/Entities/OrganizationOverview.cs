using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    /// <summary>
    /// Represents the organization-level financial summary.
    /// Provides consolidated views of contributions, fund balances,
    /// and organizational structure financial status.
    /// </summary>
    [Table("organization_overviews")]
    public class OrganizationOverview : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalOrganizationWideContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSectionContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGroupContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCommitteeContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalContributions { get; set; }

        public int ActiveSectionCount { get; set; }

        public int ActiveGroupCount { get; set; }

        public int ActiveCommitteeCount { get; set; }

        public DateTime? LastUpdatedUtc { get; set; }

        public bool IsStale { get; set; }
    }

    /// <summary>
    /// Financial breakdown by contribution type at organization level
    /// </summary>
    [Table("organization_contribution_type_summaries")]
    public class OrganizationContributionTypeSummary : BaseEntity
    {
        [Required]
        public Guid ContributionTypeId { get; set; }

        [Required]
        public Guid OrganizationOverviewId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int ContributionCount { get; set; }

        [ForeignKey(nameof(ContributionTypeId))]
        public virtual ContributionType? ContributionType { get; set; }

        [ForeignKey(nameof(OrganizationOverviewId))]
        public virtual OrganizationOverview? OrganizationOverview { get; set; }
    }

    /// <summary>
    /// Financial summary for a specific section
    /// </summary>
    [Table("section_financial_summaries")]
    public class SectionFinancialSummary : BaseEntity
    {
        [Required]
        public Guid SectionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DirectContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GroupContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommitteeContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalContributions { get; set; }

        public int ActiveGroupCount { get; set; }

        public int ActiveCommitteeCount { get; set; }

        public DateTime? LastUpdatedUtc { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual Section? Section { get; set; }
    }

    /// <summary>
    /// Financial summary for a specific group
    /// </summary>
    [Table("group_financial_summaries")]
    public class GroupFinancialSummary : BaseEntity
    {
        [Required]
        public Guid GroupId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DirectContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommitteeContributions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalContributions { get; set; }

        public int ActiveCommitteeCount { get; set; }

        public DateTime? LastUpdatedUtc { get; set; }

        [ForeignKey(nameof(GroupId))]
        public virtual Group? Group { get; set; }
    }

    /// <summary>
    /// Financial summary for a specific committee
    /// </summary>
    [Table("committee_financial_summaries")]
    public class CommitteeFinancialSummary : BaseEntity
    {
        [Required]
        public Guid CommitteeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalContributions { get; set; }

        public int ContributionCount { get; set; }

        public DateTime? LastUpdatedUtc { get; set; }

        [ForeignKey(nameof(CommitteeId))]
        public virtual Committee? Committee { get; set; }
    }
}
