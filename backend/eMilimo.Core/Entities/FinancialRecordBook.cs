using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public static class FinancialRecordBookStatuses
{
    public const string Draft = "DRAFT";
    public const string AwaitingVerification = "AWAITING_VERIFICATION";
    public const string Finalized = "FINALIZED";
    public const string Returned = "RETURNED";
    public const string Archived = "ARCHIVED";
}

public static class FrbOfficerTypes
{
    public const string StewardshipConvener = "STEWARDSHIP_CONVENER";
    public const string Treasurer = "TREASURER";
    public const string Minister = "MINISTER";
    public static readonly string[] Required = [StewardshipConvener, Treasurer, Minister];
}

[Table("financial_record_books")]
public sealed class FinancialRecordBook : BaseEntity
{
    [Required, MaxLength(50)] public string FrbNumber { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    [MaxLength(30)] public string Status { get; set; } = FinancialRecordBookStatuses.Draft;
    public string? Notes { get; set; }
    public string? ReviewerComments { get; set; }
    [Column(TypeName = "jsonb")] public string? AssessmentRuleIdsJson { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal GrossContributions { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal AssessmentTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal NetPosition { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public Guid? SubmittedById { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public Guid? FinalizedById { get; set; }
    public ICollection<FinancialRecordBookContribution> Contributions { get; set; } = new List<FinancialRecordBookContribution>();
    public ICollection<FrbAssessmentSnapshot> AssessmentSnapshots { get; set; } = new List<FrbAssessmentSnapshot>();
    public ICollection<FrbVerification> Verifications { get; set; } = new List<FrbVerification>();
    public ICollection<FinancialRecordBookServiceCollection> ServiceCollections { get; set; } = new List<FinancialRecordBookServiceCollection>();
}

[Table("financial_record_book_contributions")]
public sealed class FinancialRecordBookContribution : BaseEntity
{
    public Guid FinancialRecordBookId { get; set; }
    public FinancialRecordBook? FinancialRecordBook { get; set; }
    public Guid ContributionId { get; set; }
    public Contribution? Contribution { get; set; }
    [MaxLength(50)] public string? ContributionNumberSnapshot { get; set; }
    public DateOnly? ContributionDateSnapshot { get; set; }
    public Guid? ContributionTypeIdSnapshot { get; set; }
    [MaxLength(100)] public string? ContributionTypeNameSnapshot { get; set; }
    public string? DescriptionSnapshot { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? AmountSnapshot { get; set; }
    [MaxLength(20)] public string? StatusSnapshot { get; set; }
    [Column(TypeName = "jsonb")] public string? LinesSnapshotJson { get; set; }
}

[Table("frb_assessment_rules")]
public sealed class FrbAssessmentRule : BaseEntity
{
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required, MaxLength(30)] public string Classification { get; set; } = "DEDUCTION";
    [Required, MaxLength(30)] public string CalculationMethod { get; set; } = "PERCENTAGE";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public ICollection<FrbAssessmentRuleContributionType> ContributionTypes { get; set; } = new List<FrbAssessmentRuleContributionType>();
    public ICollection<FrbAssessmentAllocationLine> AllocationLines { get; set; } = new List<FrbAssessmentAllocationLine>();
}

[Table("frb_assessment_allocation_lines")]
public sealed class FrbAssessmentAllocationLine : BaseEntity
{
    public Guid FrbAssessmentRuleId { get; set; }
    public FrbAssessmentRule? FrbAssessmentRule { get; set; }
    [Required, MaxLength(150)] public string Beneficiary { get; set; } = string.Empty;
    [Column(TypeName = "decimal(9,4)")] public decimal? Rate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? FixedAmount { get; set; }
    public bool IsLocalRetained { get; set; }
    public int DisplayOrder { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

[Table("frb_assessment_rule_contribution_types")]
public sealed class FrbAssessmentRuleContributionType
{
    public Guid FrbAssessmentRuleId { get; set; }
    public FrbAssessmentRule? FrbAssessmentRule { get; set; }
    public Guid ContributionTypeId { get; set; }
    public ContributionType? ContributionType { get; set; }
}

[Table("frb_assessment_snapshots")]
public sealed class FrbAssessmentSnapshot : BaseEntity
{
    public Guid FinancialRecordBookId { get; set; }
    public FinancialRecordBook? FinancialRecordBook { get; set; }
    public Guid SourceRuleId { get; set; }
    [MaxLength(150)] public string RuleName { get; set; } = string.Empty;
    [MaxLength(30)] public string Classification { get; set; } = string.Empty;
    [MaxLength(30)] public string CalculationMethod { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal AssessmentBase { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CalculatedAmount { get; set; }
    public string ContributionTypeIdsJson { get; set; } = "[]";
    public string ContributionTypeNamesJson { get; set; } = "[]";
    public ICollection<FrbAssessmentAllocationSnapshot> AllocationLines { get; set; } = new List<FrbAssessmentAllocationSnapshot>();
}

[Table("frb_assessment_allocation_snapshots")]
public sealed class FrbAssessmentAllocationSnapshot : BaseEntity
{
    public Guid FrbAssessmentSnapshotId { get; set; }
    public FrbAssessmentSnapshot? FrbAssessmentSnapshot { get; set; }
    public Guid? SourceAllocationLineId { get; set; }
    [Required, MaxLength(150)] public string Beneficiary { get; set; } = string.Empty;
    [Column(TypeName = "decimal(9,4)")] public decimal? Rate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? FixedAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CalculatedAmount { get; set; }
    public bool IsLocalRetained { get; set; }
    public int DisplayOrder { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

[Table("frb_verifications")]
public sealed class FrbVerification : BaseEntity
{
    public Guid FinancialRecordBookId { get; set; }
    public FinancialRecordBook? FinancialRecordBook { get; set; }
    [MaxLength(40)] public string OfficerType { get; set; } = string.Empty;
    public Guid VerifiedById { get; set; }
    [MaxLength(160)] public string OfficerName { get; set; } = string.Empty;
    [MaxLength(50)] public string OfficerRole { get; set; } = string.Empty;
    public string SignatureData { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime VerifiedAtUtc { get; set; }
}

[Table("frb_aud")]
public sealed class FrbAud
{
    public Guid Id { get; set; }
    public Guid FinancialRecordBookId { get; set; }
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}
