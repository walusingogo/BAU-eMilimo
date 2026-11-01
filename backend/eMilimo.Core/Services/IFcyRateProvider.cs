namespace eMilimo.Core.Services;

public interface IFcyRateProvider
{
    string ProviderCode { get; }
    Task<FcyProviderTable> GetLatestAsync(CancellationToken ct = default);
    Task<FcyProviderHealth> TestAsync(CancellationToken ct = default);
}

public sealed record FcyProviderRate(
    string FromCurrencyCode,
    string ToCurrencyCode,
    decimal Rate,
    string RateType,
    bool Derived,
    DateOnly? RateDate = null,
    string? SourceReference = null,
    string? Attribution = null);
public sealed record FcyProviderTable(DateOnly RateDate, bool Stale, string SourceReference, string Attribution, IReadOnlyList<FcyProviderRate> Rates);
public sealed record FcyProviderHealth(bool Available, string Message, DateTime CheckedAtUtc, DateOnly? RateDate = null, bool? Stale = null);
