// backend/eMilimo.Core/Entities/ContributionType.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    [Table("contribution_types")]
    public class ContributionType : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public bool IsDefaultServiceCollectionColumn { get; set; }

        // Digital Giving Configuration
        public bool IsEnabledForDigitalGiving { get; set; } = false;
        
        [MaxLength(30)]
        public string? DefaultDigitalDestinationType { get; set; }
        
        public bool RequireDestinationSelectionForDigitalGiving { get; set; } = false;
        
        [MaxLength(30)]
        public string DigitalDestinationEligibilityPolicy { get; set; } = ContributionDestinationEligibilityPolicies.AsConfigured;
        
        public bool AllowOrganizationWideAllocation { get; set; } = false;
        
        public bool AllowSectionAllocation { get; set; } = false;
        
        public bool AllowGroupAllocation { get; set; } = false;
        
        public bool AllowCommitteeAllocation { get; set; } = false;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumAmount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumAmount { get; set; }
        
        public int PublicDisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
        public virtual ICollection<ContributionTypeDestinationPolicy>? DestinationPolicies { get; set; } = new List<ContributionTypeDestinationPolicy>();
    }

    /// <summary>
    /// Defines destination eligibility for a contribution type.
    /// Used to restrict or allow specific organizational destinations.
    /// </summary>
    [Table("contribution_type_destination_policies")]
    public class ContributionTypeDestinationPolicy : BaseEntity
    {
        [Required]
        public Guid ContributionTypeId { get; set; }

        [Required]
        [MaxLength(30)]
        public string DestinationType { get; set; } = string.Empty;

        public Guid? SectionId { get; set; }

        public Guid? GroupId { get; set; }

        public Guid? CommitteeId { get; set; }

        public bool IsEligible { get; set; } = true;

        [ForeignKey(nameof(ContributionTypeId))]
        public virtual ContributionType? ContributionType { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual Section? Section { get; set; }

        [ForeignKey(nameof(GroupId))]
        public virtual Group? Group { get; set; }

        [ForeignKey(nameof(CommitteeId))]
        public virtual Committee? Committee { get; set; }
    }

    /// <summary>
    /// Static constants for destination eligibility policies
    /// </summary>
    public static class ContributionDestinationEligibilityPolicies
    {
        public const string AsConfigured = "AS_CONFIGURED";
        public const string AllDestinations = "ALL_DESTINATIONS";
        public const string MemberAssociated = "MEMBER_ASSOCIATED";
    }
}
