using System.ComponentModel.DataAnnotations;

namespace eMilimo.Core.DTOs;

public sealed record SaveServiceEventRequest(Guid ServiceTypeId, DateTime StartsAtUtc, string? SequenceName,
    string? Description, string? Location, Guid? SectionId, Guid? GroupId, Guid? CommitteeId, int? Attendance,
    int? ChildrenAttendance, int? TeacherAttendance, string? OfficiatingMinister, int? Version);
public sealed record ServiceEventStatusRequest(string Status, string? Reason, int Version);

public sealed class SaveServiceCollectionRequest
{
    public Guid ServiceEventId { get; set; }
    public Guid ContributionSourceId { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? CommitteeId { get; set; }
    [MaxLength(2000)] public string? Notes { get; set; }
    [MaxLength(1000)] public string? VarianceExplanation { get; set; }
    public int? Version { get; set; }
    public List<ServiceCollectionEntryRequest> Entries { get; set; } = [];
    public List<ServiceCollectionDenominationRequest> Denominations { get; set; } = [];
    public List<ServiceCollectionTenderRequest> Tenders { get; set; } = [];
    public List<ServiceCollectionParticipantRequest> Participants { get; set; } = [];
}

public sealed record ServiceCollectionEntryRequest(string PartyType, Guid? MemberId, string? ContributorName,
    string? ContributorReference, string? Notes, List<ServiceCollectionEntryLineRequest> Lines);
public sealed record ServiceCollectionEntryLineRequest(Guid ContributionTypeId, decimal Amount, string? Notes);
public sealed record ServiceCollectionDenominationRequest(string CurrencyCode, decimal Denomination, int Quantity,
    decimal ExchangeRate, Guid? ExchangeRateId, bool IsManualRateOverride, string? OverrideReason);
public sealed record ServiceCollectionTenderRequest(Guid PaymentMethodDefinitionId, string CurrencyCode, decimal OriginalAmount,
    decimal ExchangeRate, Guid? ExchangeRateId, bool IsManualRateOverride, string? OverrideReason, string? Reference, Guid? BankAccountId);
public sealed record ServiceCollectionParticipantRequest(Guid UserId, string Responsibility);
public sealed record ServiceCollectionDecisionRequest(string? Comments, int Version);
public sealed record ServiceCollectionArchiveRequest(string Reason, int Version);
public sealed record ServiceCollectionReverseRequest(string Reason, int Version);
public sealed record SavePaymentMethodDefinitionRequest(string Code, string Name, string Category, bool RequiresReference,
    bool RequiresBankAccount, bool RequiresAttachment, bool SupportsForeignCurrency, bool IsActive, int DisplayOrder, int? Version);
