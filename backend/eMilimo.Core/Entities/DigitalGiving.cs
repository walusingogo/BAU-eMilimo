using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;
using eMilimo.Core.Entities.Companions;
using eMilimo.Core.Payments;

namespace eMilimo.Core.Entities;

[Table("digital_giving_configurations")]
public sealed class DigitalGivingConfiguration : BaseEntity
{
    public bool IsEnabled { get; set; }
    [MaxLength(80)] public string PublicSlug { get; set; } = string.Empty;
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,2)")] public decimal MinimumAmount { get; set; } = 1m;
    [Column(TypeName = "decimal(18,2)")] public decimal? MaximumAmount { get; set; }
    public Guid ContributionSourceId { get; set; }
    public ContributionSource? ContributionSource { get; set; }
    [MaxLength(250)] public string? WelcomeMessage { get; set; }
    public ICollection<DigitalGivingTypeSetting> ContributionTypes { get; set; } = new List<DigitalGivingTypeSetting>();
}

[Table("digital_giving_type_settings")]
public sealed class DigitalGivingTypeSetting : BaseEntity
{
    public Guid DigitalGivingConfigurationId { get; set; }
    public DigitalGivingConfiguration? DigitalGivingConfiguration { get; set; }
    public Guid ContributionTypeId { get; set; }
    public ContributionType? ContributionType { get; set; }
    public bool IsPubliclyAvailable { get; set; } = true;
    public int DisplayOrder { get; set; }
    
    // Destination configuration: applies only if type requires destination selection
    [MaxLength(20)] public string DestinationType { get; set; } = DigitalGivingDestinationTypes.Organization;
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? CommitteeId { get; set; }
    public Committee? Committee { get; set; }
    
    // Allow multiple destinations for flexible giving types
    public bool AllowDestinationSelection { get; set; } = false;
    
    [MaxLength(30)]
    public string DestinationEligibilityPolicy { get; set; } = DigitalGivingDestinationEligibilityPolicies.AsConfigured;
}

public static class DigitalGivingDestinationTypes
{
    public const string Organization = "ORGANIZATION";
    public const string Section = "SECTION";
    public const string Group = "GROUP";
    public const string Committee = "COMMITTEE";
    public static readonly string[] All = [Organization, Section, Group, Committee];
}

public static class DigitalGivingDestinationEligibilityPolicies
{
    public const string AsConfigured = "AS_CONFIGURED";
    public const string AllDestinations = "ALL_DESTINATIONS";
    public const string MemberAssociated = "MEMBER_ASSOCIATED";
}

[Table("integration_configurations")]
public sealed class PaymentProviderConfiguration : BaseEntity
{
    [MaxLength(80)] public string ProviderKey { get; set; } = string.Empty;
    [MaxLength(120)] public string DisplayName { get; set; } = string.Empty;
    [MaxLength(30)] public string ProviderKind { get; set; } = PaymentProviderKinds.Direct;
    [MaxLength(30)] public string Environment { get; set; } = "SANDBOX";
    public bool IsEnabled { get; set; }
    public string ProtectedConfiguration { get; set; } = string.Empty;
    public int ConfigurationVersion { get; set; } = 1;
    public bool IsValidated { get; set; }
    public DateTime? LastValidatedAtUtc { get; set; }
    public ICollection<PaymentProviderRoute> Routes { get; set; } = new List<PaymentProviderRoute>();
}

[Table("integration_configuration_aud")]
public sealed class IntegrationConfigurationAud : BaseEntity
{
    public Guid IntegrationConfigurationId { get; set; }
    public PaymentProviderConfiguration? PaymentProviderConfiguration { get; set; }
    [MaxLength(30)] public string Action { get; set; } = "UPDATED";
    public int ConfigurationVersion { get; set; }
    public string ProtectedConfigurationSnapshot { get; set; } = string.Empty;
    public string SanitizedConfigurationSnapshot { get; set; } = "{}";
    public string ChangedFieldsJson { get; set; } = "[]";
    [MaxLength(30)] public string ValidationStatus { get; set; } = "NOT_TESTED";
    [MaxLength(1000)] public string? ValidationMessage { get; set; }
    public DateTime? ValidatedAtUtc { get; set; }
    [MaxLength(500)] public string? ChangeReason { get; set; }
}

[Table("payment_provider_routes")]
public sealed class PaymentProviderRoute : BaseEntity
{
    public Guid PaymentMethodDefinitionId { get; set; }
    public PaymentMethodDefinition? PaymentMethodDefinition { get; set; }
    public Guid PaymentProviderConfigurationId { get; set; }
    public PaymentProviderConfiguration? PaymentProviderConfiguration { get; set; }
    [MaxLength(80)] public string ProviderPaymentMethodCode { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;
}

[Table("digital_collections")]
public sealed class DigitalCollection : BaseEntity
{
    [MaxLength(60)] public string PaymentReference { get; set; } = string.Empty;
    [MaxLength(120)] public string IdempotencyKey { get; set; } = string.Empty;
    [MaxLength(128)] public string RequestFingerprint { get; set; } = string.Empty;
    [MaxLength(128)] public string PublicStatusTokenHash { get; set; } = string.Empty;
    public string ProtectedPublicStatusToken { get; set; } = string.Empty;
    [MaxLength(20)] public string GiverType { get; set; } = DigitalGiverTypes.Anonymous;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    [MaxLength(250)] public string? ContributorNameSnapshot { get; set; }
    public string? ProtectedPayerData { get; set; }
    public Guid PaymentMethodDefinitionId { get; set; }
    public PaymentMethodDefinition? PaymentMethodDefinition { get; set; }
    public Guid ContributionSourceId { get; set; }
    public ContributionSource? ContributionSource { get; set; }
    public Guid? PaymentProviderConfigurationId { get; set; }
    public PaymentProviderConfiguration? PaymentProviderConfiguration { get; set; }
    [MaxLength(200)] public string? ProviderTransactionId { get; set; }
    [MaxLength(30)] public string Status { get; set; } = PaymentTransactionStatuses.Created;
    [MaxLength(30)] public string SettlementStatus { get; set; } = PaymentSettlementStatuses.Pending;
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,2)")] public decimal GrossAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ProviderFee { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal NetSettlementAmount { get; set; }
    public DateTime? InitiatedAtUtc { get; set; }
    public DateTime? ProviderCompletedAtUtc { get; set; }
    public DateTime? SettledAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    [MaxLength(1000)] public string? FailureReason { get; set; }
    [MaxLength(30)] public string ReconciliationStatus { get; set; } = PaymentReconciliationStatuses.None;
    public int ReconciliationAttemptCount { get; set; }
    public DateTime? LastReconciliationAtUtc { get; set; }
    public DateTime? NextReconciliationAtUtc { get; set; }
    public int ProviderConfigurationVersion { get; set; } = 1;
    [MaxLength(30)] public string ProviderEnvironmentSnapshot { get; set; } = "SANDBOX";
    public ICollection<DigitalCollectionItem> Lines { get; set; } = new List<DigitalCollectionItem>();
    public ICollection<DigitalCollectionEvent> Events { get; set; } = new List<DigitalCollectionEvent>();
}

[Table("digital_collection_items")]
public sealed class DigitalCollectionItem : BaseEntity
{
    public Guid DigitalCollectionId { get; set; }
    public DigitalCollection? DigitalCollection { get; set; }
    public Guid ContributionTypeId { get; set; }
    public ContributionType? ContributionType { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    
    // Destination: one of these may be null to represent organization-wide allocation
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public Guid? SectionId { get; set; }
    public Section? Section { get; set; }
    public Guid? CommitteeId { get; set; }
    public Committee? Committee { get; set; }
    
    public Guid? ContributionId { get; set; }
    public Contribution? Contribution { get; set; }
}

[Table("digital_collection_atm")]
public sealed class DigitalCollectionAtm : AttachmentCompanionBase
{
    [MaxLength(50)] public string DocumentType { get; set; } = "PAYMENT_EVIDENCE";
    [MaxLength(128)] public string? ContentHash { get; set; }
}

[Table("digital_collection_lck")]
public sealed class DigitalCollectionLck : LockCompanionBase
{
    [MaxLength(30)] public string LockType { get; set; } = "PROCESSING";
    public DateTime? ReleasedAtUtc { get; set; }
    public Guid? ReleasedById { get; set; }
}

[Table("digital_collection_aud")]
public sealed class DigitalCollectionAud
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DigitalCollectionId { get; set; }
    public DigitalCollection? DigitalCollection { get; set; }
    [MaxLength(60)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
}

[Table("digital_collection_item_atm")]
public sealed class DigitalCollectionItemAtm : AttachmentCompanionBase
{
    [MaxLength(50)] public string DocumentType { get; set; } = "ALLOCATION_EVIDENCE";
    [MaxLength(128)] public string? ContentHash { get; set; }
}

[Table("digital_collection_item_lck")]
public sealed class DigitalCollectionItemLck : LockCompanionBase
{
    [MaxLength(30)] public string LockType { get; set; } = "PROCESSING";
    public DateTime? ReleasedAtUtc { get; set; }
    public Guid? ReleasedById { get; set; }
}

[Table("digital_collection_item_aud")]
public sealed class DigitalCollectionItemAud
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DigitalCollectionItemId { get; set; }
    public DigitalCollectionItem? DigitalCollectionItem { get; set; }
    [MaxLength(60)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
}

[Table("digital_collection_events")]
public sealed class DigitalCollectionEvent : BaseEntity
{
    public Guid DigitalCollectionId { get; set; }
    public DigitalCollection? DigitalCollection { get; set; }
    [MaxLength(50)] public string EventType { get; set; } = string.Empty;
    [MaxLength(30)] public string? PreviousStatus { get; set; }
    [MaxLength(30)] public string NewStatus { get; set; } = string.Empty;
    [MaxLength(80)] public string Source { get; set; } = string.Empty;
    [MaxLength(100)] public string? CorrelationId { get; set; }
    public string? ProtectedDetails { get; set; }
    public string SanitizedDetailsJson { get; set; } = "{}";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("integration_webhook_events")]
public sealed class PaymentWebhookEvent : BaseEntity
{
    public Guid PaymentProviderConfigurationId { get; set; }
    public PaymentProviderConfiguration? PaymentProviderConfiguration { get; set; }
    public Guid? DigitalCollectionId { get; set; }
    public DigitalCollection? DigitalCollection { get; set; }
    [MaxLength(200)] public string EventKey { get; set; } = string.Empty;
    [MaxLength(128)] public string PayloadHash { get; set; } = string.Empty;
    public bool SignatureValid { get; set; }
    [MaxLength(30)] public string ProcessingStatus { get; set; } = "RECEIVED";
    [MaxLength(100)] public string? CorrelationId { get; set; }
    [MaxLength(1000)] public string? FailureReason { get; set; }
    public string? ProtectedPayload { get; set; }
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public int ProcessingAttempts { get; set; }
}

[Table("integration_exchanges")]
public sealed class PaymentApiExchange : BaseEntity
{
    public Guid PaymentProviderConfigurationId { get; set; }
    public PaymentProviderConfiguration? PaymentProviderConfiguration { get; set; }
    public Guid? DigitalCollectionId { get; set; }
    public DigitalCollection? DigitalCollection { get; set; }
    public Guid? MtnSandboxTestExecutionId { get; set; }
    public MtnSandboxTestExecution? MtnSandboxTestExecution { get; set; }
    public int ConfigurationVersion { get; set; }
    [MaxLength(30)] public string Environment { get; set; } = "SANDBOX";
    [MaxLength(50)] public string Operation { get; set; } = string.Empty;
    [MaxLength(10)] public string HttpMethod { get; set; } = string.Empty;
    [MaxLength(500)] public string Endpoint { get; set; } = string.Empty;
    [MaxLength(80)] public string? TransactionReference { get; set; }
    public string SanitizedRequestHeaders { get; set; } = "{}";
    public string? SanitizedRequestPayload { get; set; }
    public int? HttpStatusCode { get; set; }
    public string SanitizedResponseHeaders { get; set; } = "{}";
    public string? SanitizedResponsePayload { get; set; }
    public long DurationMilliseconds { get; set; }
    public int AttemptNumber { get; set; } = 1;
    [MaxLength(50)] public string? ErrorCategory { get; set; }
    [MaxLength(100)] public string? CorrelationId { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
}

[Table("mtn_sandbox_scenarios")]
public sealed class MtnSandboxScenario : BaseEntity
{
    [MaxLength(80)] public string ScenarioCode { get; set; } = string.Empty;
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    [MaxLength(20)] public string Msisdn { get; set; } = string.Empty;
    [MaxLength(30)] public string ExpectedTransactionStatus { get; set; } = PaymentTransactionStatuses.Pending;
    public int? ExpectedHttpResponseCode { get; set; }
    [MaxLength(100)] public string? ExpectedMtnErrorCode { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TestAmount { get; set; } = 1m;
    [MaxLength(3)] public string Currency { get; set; } = "EUR";
    [MaxLength(40)] public string ExternalReferencePrefix { get; set; } = "EMILIMO-SBX";
    [MaxLength(160)] public string PayerMessage { get; set; } = "eMilimo sandbox test";
    [MaxLength(160)] public string PayeeNote { get; set; } = "eMilimo sandbox test";
    [MaxLength(50)] public string ExpectedProcessingBehaviour { get; set; } = "TERMINAL_FAILURE";
    public int MaximumPollingAttempts { get; set; } = 12;
    public int PollingIntervalSeconds { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    [MaxLength(30)] public string SourceClassification { get; set; } = "DOCUMENTED";
    [MaxLength(500)] public string? SourceReference { get; set; }
    public bool IsSystemDefault { get; set; }
    public int ConfigurationVersion { get; set; } = 1;
    public string SamplePresetsJson { get; set; } = "[]";
    public ICollection<MtnSandboxTestExecution> Executions { get; set; } = new List<MtnSandboxTestExecution>();
}

[Table("mtn_sandbox_test_executions")]
public sealed class MtnSandboxTestExecution : BaseEntity
{
    public Guid PaymentProviderConfigurationId { get; set; }
    public PaymentProviderConfiguration? PaymentProviderConfiguration { get; set; }
    public Guid MtnSandboxScenarioId { get; set; }
    public MtnSandboxScenario? MtnSandboxScenario { get; set; }
    public Guid? SamplePresetId { get; set; }
    public string? SamplePresetSnapshotJson { get; set; }
    public Guid TransactionUuid { get; set; }
    [MaxLength(120)] public string ExternalReference { get; set; } = string.Empty;
    [MaxLength(20)] public string TestMsisdn { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "EUR";
    [MaxLength(30)] public string ExpectedStatus { get; set; } = PaymentTransactionStatuses.Pending;
    [MaxLength(100)] public string? ExpectedErrorCode { get; set; }
    public int? ExpectedHttpStatusCode { get; set; }
    [MaxLength(30)] public string ActualStatus { get; set; } = PaymentTransactionStatuses.Created;
    [MaxLength(100)] public string? ActualErrorCode { get; set; }
    public int? ActualHttpStatusCode { get; set; }
    [MaxLength(30)] public string Verdict { get; set; } = "PENDING";
    [MaxLength(30)] public string ExecutionStatus { get; set; } = "CREATED";
    public int ProviderConfigurationVersion { get; set; }
    public int PollingAttemptCount { get; set; }
    public int MaximumPollingAttempts { get; set; }
    public int PollingIntervalSeconds { get; set; }
    public DateTime? NextPollAtUtc { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    [MaxLength(1000)] public string? ProviderMessage { get; set; }
    [MaxLength(100)] public string? CorrelationId { get; set; }
    public string SanitizedRequestPreview { get; set; } = "{}";
    public string TimelineJson { get; set; } = "[]";
    public ICollection<PaymentApiExchange> ApiExchanges { get; set; } = new List<PaymentApiExchange>();
}

[Table("member_verification_challenges")]
public sealed class MemberVerificationChallenge : BaseEntity
{
    [MaxLength(128)] public string LookupHash { get; set; } = string.Empty;
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }
    [MaxLength(128)] public string ClientFingerprintHash { get; set; } = string.Empty;
    public byte[] OtpSalt { get; set; } = [];
    public byte[] OtpHash { get; set; } = [];
    public int AttemptCount { get; set; }
    public int MaximumAttempts { get; set; } = 5;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    [MaxLength(128)] public string? VerificationTokenHash { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    [MaxLength(30)] public string DeliveryChannel { get; set; } = "NONE";
    [MaxLength(80)] public string? DestinationHint { get; set; }
}

[Table("financial_adjustments")]
public sealed class FinancialAdjustment : BaseEntity
{
    public Guid DigitalCollectionId { get; set; }
    public DigitalCollection? DigitalCollection { get; set; }

    public Guid? RelatedAdjustmentId { get; set; }
    public FinancialAdjustment? RelatedAdjustment { get; set; }

    [MaxLength(40)]
    public string AdjustmentType { get; set; } = FinancialAdjustmentTypes.AccountingReversal;
    
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(30)]
    public string Status { get; set; } = FinancialAdjustmentStatuses.Requested;

    public DateTime? CompletedAtUtc { get; set; }

    public Guid? CompletedById { get; set; }
    public User? CompletedBy { get; set; }
}
