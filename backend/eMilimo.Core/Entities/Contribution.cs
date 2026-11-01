using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;
using eMilimo.Core.Entities.Companions;

namespace eMilimo.Core.Entities;

[Table("contributions")]
public class Contribution : BaseEntity
{
    [Required, MaxLength(50)] public string ContributionNumber { get; set; } = string.Empty;
    [Required] public Guid ContributionTypeId { get; set; }
    [Required] public Guid SourceId { get; set; }
    [Required, Column(TypeName = "decimal(15,2)")] public decimal Amount { get; set; }
    [Required] public DateTime ContributionDate { get; set; }
    public string? Description { get; set; }
    [MaxLength(100)] public string? ReferenceNumber { get; set; }
    [Required, MaxLength(50)] public string PaymentMethod { get; set; } = string.Empty;
    public Guid? BankAccountId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "PENDING";
    public bool IsSplit { get; set; }
    public Guid? ReceivedBy { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public Guid? ArchivedBy { get; set; }
    public string? RejectionReason { get; set; }
    public string? ArchiveReason { get; set; }
    public Guid? FinalizedFinancialRecordBookId { get; set; }

    [MaxLength(30)] public string OriginType { get; set; } = "MANUAL";
    public Guid? AssetCheckoutId { get; set; }
    public Guid? MemberId { get; set; }
    [MaxLength(100)] public string? MembershipNumberSnapshot { get; set; }
    [MaxLength(250)] public string? ContributorNameSnapshot { get; set; }
    public Guid? PaymentMethodDefinitionId { get; set; }
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,4)")] public decimal OriginalCurrencyAmount { get; set; }
    [Column(TypeName = "decimal(28,12)")] public decimal ExchangeRateSnapshot { get; set; } = 1m;
    public Guid? ReversesContributionId { get; set; }

    [ForeignKey(nameof(ContributionTypeId))] public virtual ContributionType? ContributionType { get; set; }
    [ForeignKey(nameof(BankAccountId))] public virtual BankAccount? BankAccount { get; set; }
    [ForeignKey(nameof(SourceId))] public virtual ContributionSource? Source { get; set; }
    [ForeignKey(nameof(ReceivedBy))] public virtual User? ReceivedByUser { get; set; }
    [ForeignKey(nameof(ConfirmedBy))] public virtual User? ConfirmedByUser { get; set; }
    [ForeignKey(nameof(CreatedById))] public virtual User? CreatedByUser { get; set; }
    [ForeignKey(nameof(UpdatedById))] public virtual User? UpdatedByUser { get; set; }
    [ForeignKey(nameof(MemberId))] public virtual Member? Member { get; set; }
    [ForeignKey(nameof(PaymentMethodDefinitionId))] public virtual PaymentMethodDefinition? PaymentMethodDefinition { get; set; }
    [ForeignKey(nameof(AssetCheckoutId))] public virtual AssetCheckout? AssetCheckout { get; set; }
    public virtual ServiceCollectionEntryLine? ServiceCollectionEntryLine { get; set; }
    public virtual ICollection<ContributionAllocation> Allocations { get; set; } = new List<ContributionAllocation>();
    public virtual ICollection<ContributionDenomination> Denominations { get; set; } = new List<ContributionDenomination>();
    public virtual ICollection<ContributionAtm> Attachments { get; set; } = new List<ContributionAtm>();
    public virtual ICollection<ContributionLck> Locks { get; set; } = new List<ContributionLck>();
}
