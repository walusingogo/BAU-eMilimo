namespace eMilimo.Core.DTOs;

public sealed record SaveFinancialRecordBookRequest(
    DateTime PeriodStart, DateTime PeriodEnd, Guid? SectionId, string? Notes,
    IReadOnlyCollection<Guid> ContributionIds,
    IReadOnlyCollection<Guid>? ServiceCollectionIds = null,
    IReadOnlyCollection<Guid>? AssessmentRuleIds = null);

public sealed record FrbVerificationRequest(string OfficerType, string? Comments);

public sealed record ArchiveFinancialRecordBookRequest(string Reason);

public sealed record SaveFrbAssessmentAllocationLineRequest(
    string Beneficiary, decimal? Rate, decimal? FixedAmount, bool IsLocalRetained, int DisplayOrder, string? Notes);

public sealed record SaveFrbAssessmentRuleRequest(
    string Name, string? Description, string Classification, string CalculationMethod,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive, string? Notes,
    IReadOnlyCollection<Guid> ContributionTypeIds,
    IReadOnlyCollection<SaveFrbAssessmentAllocationLineRequest> AllocationLines);

public sealed record SaveReferenceNumberConfigurationRequest(
    string ReferenceType, string Prefix, string Description, DateOnly EffectiveFrom, bool IsActive);
