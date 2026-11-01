using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public sealed class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FcyManagementSetting : BaseEntity
{
    public bool IsEnabled { get; set; }
    public string BaseCurrencyCode { get; set; } = "ZMW";
    public string RateSourceMode { get; set; } = "MANUAL_ONLY";
    public string? ProviderCode { get; set; }
    public bool AllowManualRateOverride { get; set; }
    public bool RequireApprovalForManualOverride { get; set; } = true;
    public bool AllowLastKnownRateFallback { get; set; } = true;
    public int MaximumRateAgeHours { get; set; } = 24;
    public decimal OverrideTolerancePercent { get; set; } = 2m;
}

public sealed class ExchangeRate : BaseEntity
{
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string SourceKind { get; set; } = "MANUAL";
    public string SourceReference { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
}
