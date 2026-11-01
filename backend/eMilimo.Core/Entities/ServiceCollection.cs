using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;
using eMilimo.Core.Entities.Companions;

namespace eMilimo.Core.Entities;

public static class ServiceCollectionStatuses
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Returned = "RETURNED";
    public const string Approved = "APPROVED";
    public const string Archived = "ARCHIVED";
    public const string Reversed = "REVERSED";
}

public static class ServiceEventStatuses
{
    public const string Active = "OPEN";
    public const string Disabled = "DISABLED";
    public const string Archived = "ARCHIVED";
}

[Table("service_types")]
public sealed class ServiceType : BaseEntity
{
    [MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

[Table("service_events")]
public sealed class ServiceEvent : BaseEntity
{
    public Guid ServiceTypeId { get; set; }
    public ServiceType? ServiceType { get; set; }
    public DateTime StartsAtUtc { get; set; }
    [MaxLength(120)] public string? SequenceName { get; set; }
    [MaxLength(200)] public string? Description { get; set; }
    [MaxLength(200)] public string? Location { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public Guid? CommitteeId { get; set; }
    public Committee? Committee { get; set; }
    public int? Attendance { get; set; }
    public int? ChildrenAttendance { get; set; }
    public int? TeacherAttendance { get; set; }
    [MaxLength(200)] public string? OfficiatingMinister { get; set; }
    [MaxLength(30)] public string Status { get; set; } = ServiceEventStatuses.Active;
}

[Table("payment_method_definitions")]
public sealed class PaymentMethodDefinition : BaseEntity
{
    [MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(30)] public string Category { get; set; } = "OTHER";
    public bool RequiresReference { get; set; }
    public bool RequiresBankAccount { get; set; }
    public bool RequiresAttachment { get; set; }
    public bool SupportsForeignCurrency { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

[Table("service_collections")]
public sealed class ServiceCollection : BaseEntity
{
    [MaxLength(50)] public string CollectionNumber { get; set; } = string.Empty;
    public Guid ServiceEventId { get; set; }
    public ServiceEvent? ServiceEvent { get; set; }
    public Guid ContributionSourceId { get; set; }
    public ContributionSource? ContributionSource { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public Guid? CommitteeId { get; set; }
    public Committee? Committee { get; set; }
    [MaxLength(3)] public string BaseCurrencyCode { get; set; } = "ZMW";
    [MaxLength(30)] public string Status { get; set; } = ServiceCollectionStatuses.Draft;
    [Column(TypeName = "decimal(18,2)")] public decimal PurposeTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TenderTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Variance { get; set; }
    [MaxLength(2000)] public string? Notes { get; set; }
    [MaxLength(1000)] public string? VarianceExplanation { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public Guid? SubmittedById { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public Guid? ReturnedById { get; set; }
    [MaxLength(1000)] public string? ReviewComments { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public Guid? ArchivedById { get; set; }
    [MaxLength(500)] public string? ArchiveReason { get; set; }
    public Guid? FinalizedFinancialRecordBookId { get; set; }
    public ICollection<ServiceCollectionEntry> Entries { get; set; } = new List<ServiceCollectionEntry>();
    public ICollection<ServiceCollectionDenomination> Denominations { get; set; } = new List<ServiceCollectionDenomination>();
    public ICollection<ServiceCollectionTender> Tenders { get; set; } = new List<ServiceCollectionTender>();
    public ICollection<ServiceCollectionParticipant> Participants { get; set; } = new List<ServiceCollectionParticipant>();
}

[Table("service_collection_entries")]
public sealed class ServiceCollectionEntry : BaseEntity
{
    public Guid ServiceCollectionId { get; set; }
    public ServiceCollection? ServiceCollection { get; set; }
    [MaxLength(30)] public string PartyType { get; set; } = "ANONYMOUS";
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    [MaxLength(100)] public string? MembershipNumberSnapshot { get; set; }
    [MaxLength(250)] public string ContributorNameSnapshot { get; set; } = "Anonymous";
    [MaxLength(100)] public string? ContributorReference { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public ICollection<ServiceCollectionEntryLine> Lines { get; set; } = new List<ServiceCollectionEntryLine>();
}

[Table("service_collection_entry_lines")]
public sealed class ServiceCollectionEntryLine : BaseEntity
{
    public Guid ServiceCollectionEntryId { get; set; }
    public ServiceCollectionEntry? ServiceCollectionEntry { get; set; }
    public Guid ContributionTypeId { get; set; }
    public ContributionType? ContributionType { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public Guid? ContributionId { get; set; }
    public Contribution? Contribution { get; set; }
}

[Table("service_collection_denominations")]
public sealed class ServiceCollectionDenomination : BaseEntity
{
    public Guid ServiceCollectionId { get; set; }
    public ServiceCollection? ServiceCollection { get; set; }
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,4)")] public decimal Denomination { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal OriginalAmount { get; set; }
    [Column(TypeName = "decimal(28,12)")] public decimal ExchangeRate { get; set; } = 1m;
    [Column(TypeName = "decimal(18,2)")] public decimal BaseEquivalent { get; set; }
    public Guid? ExchangeRateId { get; set; }
    public bool IsManualRateOverride { get; set; }
    [MaxLength(500)] public string? OverrideReason { get; set; }
}

[Table("service_collection_tenders")]
public sealed class ServiceCollectionTender : BaseEntity
{
    public Guid ServiceCollectionId { get; set; }
    public ServiceCollection? ServiceCollection { get; set; }
    public Guid PaymentMethodDefinitionId { get; set; }
    public PaymentMethodDefinition? PaymentMethodDefinition { get; set; }
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,4)")] public decimal OriginalAmount { get; set; }
    [Column(TypeName = "decimal(28,12)")] public decimal ExchangeRate { get; set; } = 1m;
    [Column(TypeName = "decimal(18,2)")] public decimal BaseEquivalent { get; set; }
    public Guid? ExchangeRateId { get; set; }
    public bool IsManualRateOverride { get; set; }
    [MaxLength(500)] public string? OverrideReason { get; set; }
    [MaxLength(200)] public string? Reference { get; set; }
    public Guid? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
}

[Table("service_collection_participants")]
public sealed class ServiceCollectionParticipant : BaseEntity
{
    public Guid ServiceCollectionId { get; set; }
    public ServiceCollection? ServiceCollection { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(50)] public string Responsibility { get; set; } = "COUNTER";
    public string? SignatureData { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

[Table("service_collection_atm")]
public sealed class ServiceCollectionAtm : AttachmentCompanionBase
{
    [MaxLength(50)] public string DocumentType { get; set; } = "SUPPORTING_EVIDENCE";
    [MaxLength(128)] public string? ContentHash { get; set; }
}

[Table("service_collection_lck")]
public sealed class ServiceCollectionLck : LockCompanionBase
{
    [MaxLength(30)] public string LockType { get; set; } = "WORKFLOW";
    public DateTime? ReleasedAtUtc { get; set; }
    public Guid? ReleasedById { get; set; }
}

[Table("service_collection_aud")]
public sealed class ServiceCollectionAud
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceCollectionId { get; set; }
    [MaxLength(60)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
}

[Table("financial_record_book_service_collections")]
public sealed class FinancialRecordBookServiceCollection : BaseEntity
{
    public Guid FinancialRecordBookId { get; set; }
    public FinancialRecordBook? FinancialRecordBook { get; set; }
    public Guid ServiceCollectionId { get; set; }
    public ServiceCollection? ServiceCollection { get; set; }
    [MaxLength(50)] public string CollectionNumberSnapshot { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal PurposeTotalSnapshot { get; set; }
    public DateTime? ServiceDateSnapshot { get; set; }
    [MaxLength(30)] public string? StatusSnapshot { get; set; }
    [Column(TypeName = "jsonb")] public string? LinesSnapshotJson { get; set; }
}
