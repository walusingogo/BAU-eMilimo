// backend/eMilimo.Core/Entities/FundTransaction.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    [Table("fund_transactions")]
    public class FundTransaction : BaseEntity
    {
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FundType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal PreviousBalance { get; set; }

        [Required]
        [Column(TypeName = "decimal(15,2)")]
        public decimal NewBalance { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Source { get; set; }

        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        public Guid? ReferenceId { get; set; }

        [MaxLength(180)]
        public string? PostingKey { get; set; }

        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        public bool IsReversed { get; set; } = false;

        public Guid? ReversedBy { get; set; }

        public DateTime? ReversedAt { get; set; }

        public string? ReversalReason { get; set; }

        // Navigation properties
        [ForeignKey(nameof(GroupId))]
        public virtual Group? Group { get; set; }
        [ForeignKey(nameof(SectionId))]
        public virtual Section? Section { get; set; }
        [ForeignKey(nameof(CommitteeId))]
        public virtual Committee? Committee { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedById))]
        public virtual User? UpdatedByUser { get; set; }
    }
}
