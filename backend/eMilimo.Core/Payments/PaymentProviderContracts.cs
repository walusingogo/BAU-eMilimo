namespace eMilimo.Core.Payments;

public static class PaymentProviderKinds
{
    public const string Direct = "DIRECT";
    public const string Aggregator = "AGGREGATOR";
    public const string Sandbox = "SANDBOX";
}

public static class PaymentTransactionStatuses
{
    public const string Created = "CREATED";
    public const string Pending = "PENDING";
    public const string Processing = "PROCESSING";
    public const string Succeeded = "SUCCEEDED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
    public const string Expired = "EXPIRED";
    public const string Reversed = "REVERSED";
    public const string Unknown = "UNKNOWN";
}

public static class PaymentSettlementStatuses
{
    public const string Pending = "PENDING";
    public const string Settled = "SETTLED";
    public const string Failed = "FAILED";
    public const string Reconciled = "RECONCILED";
}

public static class PaymentReconciliationStatuses
{
    public const string None = "NONE";
    public const string Scheduled = "SCHEDULED";
    public const string Polling = "POLLING";
    public const string Recovered = "RECOVERED";
    public const string ManualReview = "MANUAL_REVIEW";
}

public static class FinancialAdjustmentTypes
{
    public const string AccountingReversal = "ACCOUNTING_REVERSAL";
}

public static class FinancialAdjustmentStatuses
{
    public const string Requested = "REQUESTED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

public static class DigitalGiverTypes
{
    public const string Member = "MEMBER";
    public const string Guest = "GUEST";
    public const string Anonymous = "ANONYMOUS";
}

public sealed record PaymentProviderDescriptor(
    string ProviderKey,
    string DisplayName,
    string ProviderKind,
    bool IsImplemented,
    bool IsProductionReady,
    IReadOnlyCollection<string> SupportedMethodCodes,
    string? UnavailableReason = null);

public sealed record PaymentInitiationRequest(
    Guid PaymentTransactionId,
    string PaymentReference,
    string PaymentMethodCode,
    decimal Amount,
    string CurrencyCode,
    string PayerAddress,
    Uri CallbackUrl,
    string IdempotencyKey,
    IReadOnlyDictionary<string, string> ProviderSettings);

public sealed record PaymentInitiationResult(
    string Status,
    string? ProviderTransactionId,
    string? ProviderMessage,
    DateTime? ExpiresAtUtc);

public sealed record PaymentStatusResult(
    string Status,
    string? ProviderTransactionId,
    decimal? ProviderFee,
    decimal? NetSettlementAmount,
    DateTime? ProviderCompletedAtUtc,
    string? ProviderMessage,
    decimal? VerifiedAmount = null,
    string? VerifiedCurrencyCode = null);

public sealed record PaymentProviderConnectionTestResult(
    bool Succeeded,
    string Message,
    int? HttpStatusCode,
    string? ErrorCategory,
    long DurationMilliseconds);

public sealed record PaymentWebhookRequest(
    string RawBody,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string> ProviderSettings);

public sealed record PaymentWebhookResult(
    bool IsAuthentic,
    string EventKey,
    string? ProviderTransactionId,
    string Status,
    decimal? Amount,
    string? CurrencyCode,
    decimal? ProviderFee,
    decimal? NetSettlementAmount,
    DateTime? ProviderCompletedAtUtc,
    string? FailureReason);

public interface IPaymentProvider
{
    PaymentProviderDescriptor Descriptor { get; }

    Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentStatusResult> QueryStatusAsync(
        string providerTransactionId,
        IReadOnlyDictionary<string, string> providerSettings,
        CancellationToken cancellationToken = default);

    Task<PaymentWebhookResult> VerifyAndParseWebhookAsync(
        PaymentWebhookRequest request,
        CancellationToken cancellationToken = default);
}

public interface IPaymentProviderDiagnostics
{
    Task<PaymentProviderConnectionTestResult> TestConnectionAsync(
        IReadOnlyDictionary<string, string> providerSettings,
        CancellationToken cancellationToken = default);
}

public interface IPaymentProviderRegistry
{
    IReadOnlyCollection<PaymentProviderDescriptor> DescribeAll();
    IPaymentProvider GetRequired(string providerKey);
}

public sealed record PaymentRouteSelection(
    Guid ProviderConfigurationId,
    string ProviderKey,
    string ProviderPaymentMethodCode,
    int Priority);

public interface IPaymentRoutingService
{
    Task<PaymentRouteSelection> SelectInitialRouteAsync(
        Guid paymentMethodDefinitionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentMethodAvailability>> GetAvailableMethodsAsync(
        CancellationToken cancellationToken = default);
}

public sealed record PaymentMethodAvailability(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string? ProviderName = null,
    string? ProviderEnvironment = null,
    string? CurrencyCode = null,
    bool IsSandbox = false);

public sealed record PaymentProviderConfigurationDraft(
    Guid? Id,
    string ProviderKey,
    string DisplayName,
    string ProviderKind,
    string Environment,
    bool IsEnabled,
    IReadOnlyDictionary<string, string> Settings);

public sealed record PaymentProviderConfigurationView(
    Guid Id,
    string ProviderKey,
    string DisplayName,
    string ProviderKind,
    string Environment,
    bool IsEnabled,
    bool IsImplemented,
    bool IsProductionReady,
    IReadOnlyCollection<string> ConfiguredSettingKeys,
    IReadOnlyDictionary<string, string> VisibleSettings,
    string? UnavailableReason,
    int ConfigurationVersion = 1,
    DateTime? LastValidatedAtUtc = null,
    bool IsValidated = false);

public interface IPaymentProviderConfigurationStore
{
    Task<IReadOnlyList<PaymentProviderConfigurationView>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<PaymentProviderConfigurationView> SaveAsync(
        PaymentProviderConfigurationDraft draft,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetRuntimeSettingsAsync(
        Guid providerConfigurationId,
        bool requireEnabled = true,
        CancellationToken cancellationToken = default);
}
