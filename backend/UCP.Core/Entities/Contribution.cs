// backend/UCP.Core/Entities/Contribution.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCP.Core.Entities.Base;
using UCP.Core.Entities.Companions;

namespace UCP.Core.Entities
{
    [Table("contributions")]
    public class Contribution : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string ContributionNumber { get; set; } = string.Empty;

        [Required]
        public Guid ContributionTypeId { get; set; }

        [Required]
        public Guid SourceId { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ContributionDate { get; set; }

        public string? Description { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        public Guid? BankAccountId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        public bool IsSplit { get; set; } = false;

        public Guid? ReceivedBy { get; set; }

        public Guid? ConfirmedBy { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        public bool IsArchived { get; set; } = false;

        public DateTime? ArchivedAt { get; set; }

        public Guid? ArchivedBy { get; set; }
        public string? RejectionReason { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ContributionTypeId))]
        public virtual ContributionType? ContributionType { get; set; }
        
        [ForeignKey(nameof(BankAccountId))]
        public virtual BankAccount? BankAccount { get; set; }

        [ForeignKey(nameof(SourceId))]
        public virtual ContributionSource? Source { get; set; }

        [ForeignKey(nameof(ReceivedBy))]
        public virtual User? ReceivedByUser { get; set; }

        [ForeignKey(nameof(ConfirmedBy))]
        public virtual User? ConfirmedByUser { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedById))]
        public virtual User? UpdatedByUser { get; set; }

        public virtual ICollection<ContributionAllocation> Allocations { get; set; } = new List<ContributionAllocation>();

        // Companion navigation properties
        public virtual ICollection<ContributionAtm> Attachments { get; set; } = new List<ContributionAtm>();
        public virtual ICollection<ContributionLck> Locks { get; set; } = new List<ContributionLck>();
    }
}