// backend/UCP.Core/Entities/ContributionAllocation.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    [Table("contribution_allocations")]
    public class ContributionAllocation : BaseEntity
    {
        [Required]
        public Guid ContributionId { get; set; }

        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }

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

        // ADD THIS NAVIGATION PROPERTY:
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedByUser { get; set; }
    }
}
