namespace eMilimo.Core.DTOs;

public sealed record SaveFcyManagementSettingRequest(
    bool IsEnabled,
    string BaseCurrencyCode,
    string RateSourceMode,
    string? ProviderCode,
    bool AllowManualRateOverride,
    bool RequireApprovalForManualOverride,
    bool AllowLastKnownRateFallback,
    int MaximumRateAgeHours,
    decimal OverrideTolerancePercent,
    int Version);

public sealed record SaveCurrencyRequest(
    string Code,
    string Name,
    string Symbol,
    int DecimalPlaces,
    bool IsActive,
    int? Version);

public sealed record SaveExchangeRateRequest(
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Rate,
    DateTime EffectiveAtUtc,
    DateTime? ExpiresAtUtc,
    string SourceReference,
    string? Reason);

public sealed record SaveFcyProviderSettingsRequest(string? ApiKey, bool UseOpenEndpoint);
