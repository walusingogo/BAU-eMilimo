// backend/eMilimo.Core/Entities/ContributionAllocation.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    [Table("contribution_allocations")]
    public class ContributionAllocation : BaseEntity
    {
        [Required]
        public Guid ContributionId { get; set; }

        // Destination: one of these may be null to represent different allocation types
        // - All null: Organization-wide allocation
        // - GroupId set: Allocated to group
        // - SectionId set: Allocated to section
        // - CommitteeId set: Allocated to committee
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }
        
        public Guid? ContributionTypeId { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal AllocatedAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PercentageAllocation { get; set; }

        public bool IsPrimary { get; set; } = false;

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ContributionId))]
        public virtual Contribution? Contribution { get; set; }

        [ForeignKey(nameof(GroupId))]
        public virtual Group? Group { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual Section? Section { get; set; }

        [ForeignKey(nameof(CommitteeId))]
        public virtual Committee? Committee { get; set; }

        [ForeignKey(nameof(ContributionTypeId))]
        public virtual ContributionType? ContributionType { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedByUser { get; set; }
    }
}
