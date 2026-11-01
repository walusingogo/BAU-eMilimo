using eMilimo.Core.Entities;

namespace eMilimo.Core.Payments;

public sealed record DigitalGivingTypeConfigurationRequest(
    Guid ContributionTypeId,
    bool IsPubliclyAvailable,
    int DisplayOrder,
    Guid? GroupId,
    Guid? SectionId,
    Guid? CommitteeId,
    string DestinationType = DigitalGivingDestinationTypes.Organization);

public sealed record PaymentProviderRouteRequest(
    Guid PaymentMethodDefinitionId,
    Guid PaymentProviderConfigurationId,
    string ProviderPaymentMethodCode,
    int Priority,
    bool IsEnabled);

public sealed record DigitalGivingConfigurationRequest(
    bool IsEnabled,
    string PublicSlug,
    string CurrencyCode,
    decimal MinimumAmount,
    decimal? MaximumAmount,
    Guid ContributionSourceId,
    string? WelcomeMessage,
    IReadOnlyList<DigitalGivingTypeConfigurationRequest> ContributionTypes,
    IReadOnlyList<PaymentProviderRouteRequest> ProviderRoutes);

public sealed record DigitalGivingConfigurationView(
    Guid Id,
    string ChurchName,
    bool IsEnabled,
    string PublicSlug,
    string CurrencyCode,
    decimal MinimumAmount,
    decimal? MaximumAmount,
    Guid ContributionSourceId,
    string? WelcomeMessage,
    IReadOnlyList<DigitalGivingTypeView> ContributionTypes,
    IReadOnlyList<PaymentMethodAvailability> PaymentMethods,
    IReadOnlyList<PublicSandboxScenarioView> SandboxScenarios);

public sealed record PublicSandboxScenarioView(
    Guid Id,
    string Name,
    string Description,
    string Msisdn,
    string ExpectedTransactionStatus,
    string ExpectedProcessingBehaviour);

public sealed record DigitalGivingTypeView(
    Guid ContributionTypeId,
    string Code,
    string Name,
    int DisplayOrder,
    string? DefaultDestinationType,
    bool RequireDestinationSelection,
    IReadOnlyList<DigitalGivingDestinationOption> Destinations);

public sealed record DigitalGivingDestinationOption(Guid? Id, string Name, string DestinationType);

public sealed record DigitalGivingTypeConfigurationView(
    Guid ContributionTypeId,
    string Code,
    string Name,
    bool IsPubliclyAvailable,
    int DisplayOrder,
    Guid? GroupId,
    Guid? SectionId,
    Guid? CommitteeId,
    string DestinationType);

public sealed record PaymentProviderRouteView(
    Guid PaymentMethodDefinitionId,
    Guid PaymentProviderConfigurationId,
    string ProviderPaymentMethodCode,
    int Priority,
    bool IsEnabled);

public sealed record DigitalGivingAdministrationView(
    Guid Id,
    bool IsEnabled,
    string PublicSlug,
    string CurrencyCode,
    decimal MinimumAmount,
    decimal? MaximumAmount,
    Guid ContributionSourceId,
    string? WelcomeMessage,
    IReadOnlyList<DigitalGivingTypeConfigurationView> ContributionTypes,
    IReadOnlyList<PaymentProviderRouteView> ProviderRoutes);

public sealed record DigitalGivingMaintenanceOption(Guid Id, string Code, string Name);

public sealed record DigitalGivingMaintenanceCatalog(
    IReadOnlyList<DigitalGivingMaintenanceOption> ContributionTypes,
    IReadOnlyList<DigitalGivingMaintenanceOption> ContributionSources,
    IReadOnlyList<DigitalGivingMaintenanceOption> Groups,
    IReadOnlyList<DigitalGivingMaintenanceOption> Sections,
    IReadOnlyList<DigitalGivingMaintenanceOption> Committees,
    IReadOnlyList<PaymentMethodAvailability> PaymentMethods);

public interface IDigitalGivingConfigurationService
{
    Task<DigitalGivingAdministrationView?> GetAdministrationViewAsync(CancellationToken cancellationToken = default);
    Task<DigitalGivingAdministrationView> SaveAsync(
        DigitalGivingConfigurationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
    Task<DigitalGivingMaintenanceCatalog> GetMaintenanceCatalogAsync(CancellationToken cancellationToken = default);
    Task<DigitalGivingConfigurationView?> GetPublicViewAsync(
        string publicSlug,
        CancellationToken cancellationToken = default);
    Task<DigitalGivingConfigurationView?> GetDefaultPublicViewAsync(
        CancellationToken cancellationToken = default);
}

public sealed record MemberVerificationRequestResult(Guid ChallengeId, DateTime ExpiresAtUtc, string Message);
public sealed record MemberVerificationResult(bool Verified, string? VerificationToken, string? MemberDisplayName, string Message);
public sealed record MemberIdentificationResult(
    string Status,
    Guid? ChallengeId,
    string? VerificationToken,
    string Message,
    string? FullName = null,
    string? MembershipNumber = null,
    string? PhoneNumber = null);
public sealed record MemberVerificationDelivery(string Channel, string? DestinationHint);

public interface IMemberVerificationNotifier
{
    Task<MemberVerificationDelivery> SendCodeAsync(
        string? phoneNumber,
        string? email,
        string memberName,
        string code,
        CancellationToken cancellationToken = default);
}

public interface IMemberVerificationService
{
    Task<MemberIdentificationResult> IdentifyAsync(
        string lookup,
        string? additionalIdentifier,
        string clientFingerprint,
        CancellationToken cancellationToken = default);
    Task<MemberVerificationRequestResult> RequestAsync(
        string lookup,
        string clientFingerprint,
        CancellationToken cancellationToken = default);
    Task<MemberVerificationResult> VerifyAsync(
        Guid challengeId,
        string code,
        string clientFingerprint,
        CancellationToken cancellationToken = default);
}

public sealed record DigitalGivingLineRequest(
    Guid ContributionTypeId,
    decimal Amount,
    string? DestinationType = null,
    Guid? DestinationId = null);

public sealed record CreateDigitalPaymentIntentRequest(
    string PublicSlug,
    string GiverType,
    Guid PaymentMethodDefinitionId,
    decimal TotalAmount,
    string CurrencyCode,
    string PaymentPhone,
    string? ContributorName,
    string? Email,
    Guid? MemberVerificationChallengeId,
    string? MemberVerificationToken,
    string IdempotencyKey,
    IReadOnlyList<DigitalGivingLineRequest> Lines);

public sealed record DigitalPaymentIntentResult(
    Guid Id,
    string PaymentReference,
    string Status,
    decimal GrossAmount,
    string CurrencyCode,
    string PublicStatusToken,
    string? ProviderMessage,
    DateTime? ExpiresAtUtc,
    bool IsIdempotentReplay,
    bool IsSandbox);

public sealed record DigitalPaymentStatusView(
    string ChurchName,
    string PaymentReference,
    string Status,
    string SettlementStatus,
    decimal GrossAmount,
    decimal ProviderFee,
    decimal NetSettlementAmount,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<DigitalPaymentStatusLine> Lines,
    bool IsSandbox);

public sealed record DigitalPaymentStatusLine(string ContributionType, decimal Amount, string? ContributionNumber);

public interface IDigitalPaymentIntentService
{
    Task<DigitalPaymentIntentResult> CreateAsync(
        CreateDigitalPaymentIntentRequest request,
        string clientFingerprint,
        CancellationToken cancellationToken = default);
    Task<DigitalPaymentStatusView> GetStatusAsync(
        string paymentReference,
        string publicStatusToken,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentWebhookProcessingResult(
    bool Accepted,
    bool Duplicate,
    string ProcessingStatus,
    string? PaymentReference,
    string Message);

public interface IPaymentWebhookProcessor
{
    Task<PaymentWebhookProcessingResult> ProcessAsync(
        string providerKey,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        string correlationId,
        CancellationToken cancellationToken = default);
    Task<bool> SendReceiptNotificationAsync(
        string paymentReference,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentConflictException(string message) : Exception(message);
