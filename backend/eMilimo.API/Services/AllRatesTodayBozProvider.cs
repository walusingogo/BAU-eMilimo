using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

public sealed record FcyProviderSecret(string ApiKey, bool UseOpenEndpoint);

public interface IFcyProviderSettings
{
    Task<(bool HasApiKey, bool UseOpenEndpoint)> ViewAsync(CancellationToken ct = default);
    Task SaveAsync(string? apiKey, bool useOpenEndpoint, CancellationToken ct = default);
    Task<FcyProviderSecret> GetAsync(CancellationToken ct = default);
}

public sealed class FcyProviderSettings(IServiceScopeFactory scopes, IDataProtectionProvider protection) : IFcyProviderSettings
{
    private readonly IDataProtector protector = protection.CreateProtector("eMilimo.FcyProvider.Secret.v1");
    public async Task<FcyProviderSecret> GetAsync(CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var value = await db.IntegrationSettings.AsNoTracking().Where(x => x.IntegrationKey == "fcy-allratestoday-boz").Select(x => x.ProtectedSettings).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(value)) return new("", true);
        try { return JsonSerializer.Deserialize<FcyProviderSecret>(protector.Unprotect(value)) ?? new("", true); }
        catch { return new("", true); }
    }
    public async Task<(bool HasApiKey, bool UseOpenEndpoint)> ViewAsync(CancellationToken ct = default) { var value = await GetAsync(ct); return (!string.IsNullOrWhiteSpace(value.ApiKey), value.UseOpenEndpoint); }
    public async Task SaveAsync(string? apiKey, bool useOpenEndpoint, CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.IntegrationSettings.FirstOrDefaultAsync(x => x.IntegrationKey == "fcy-allratestoday-boz", ct);
        var previous = await GetAsync(ct); var value = new FcyProviderSecret(string.IsNullOrWhiteSpace(apiKey) ? previous.ApiKey : apiKey.Trim(), useOpenEndpoint);
        var protectedValue = protector.Protect(JsonSerializer.Serialize(value));
        if (row == null) db.IntegrationSettings.Add(new IntegrationSetting { Id = Guid.NewGuid(), IntegrationKey = "fcy-allratestoday-boz", ProtectedSettings = protectedValue, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1 });
        else { row.ProtectedSettings = protectedValue; row.UpdatedDate = DateTime.UtcNow; row.Version++; }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AllRatesTodayBozProvider(IHttpClientFactory clients, IFcyProviderSettings settings) : IFcyRateProvider
{
    public const string Code = "ALL_RATES_TODAY_BOZ";
    private const string ReferenceUrl = "https://allratestoday.com/api/open/reference-rate";
    public string ProviderCode => Code;
    public async Task<FcyProviderTable> GetLatestAsync(CancellationToken ct = default)
    {
        var config = await settings.GetAsync(ct); var open = config.UseOpenEndpoint || string.IsNullOrWhiteSpace(config.ApiKey);
        var url = open ? "https://allratestoday.com/api/open/central-bank/boz" : "https://allratestoday.com/api/v1/central-bank/boz/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!open) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        using var response = await clients.CreateClient("fcy-provider").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"The BoZ rate provider returned HTTP {(int)response.StatusCode}.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<BozResponse>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? throw new InvalidOperationException("The BoZ rate provider returned an empty response.");
        if (!string.Equals(payload.Bank, "boz", StringComparison.OrdinalIgnoreCase) || !DateOnly.TryParse(payload.RateDate, out var date))
            throw new InvalidOperationException("The BoZ rate provider response could not be validated.");
        if (payload.Rates is null || payload.Rates.Count == 0)
            throw new InvalidOperationException("The BoZ rate provider returned no exchange rates.");
        var selected = payload.Rates.Where(x => x.Value > 0 && x.Base?.Length == 3 && x.Quote?.Length == 3)
            .GroupBy(x => $"{x.Base.ToUpperInvariant()}:{x.Quote.ToUpperInvariant()}")
            .Select(x => x.OrderBy(y => Rank(y.Type)).First()).ToList();
        var attribution = payload.Attribution?.Source ?? "Bank of Zambia rates served by AllRatesToday";
        var rates = selected.Select(x => new FcyProviderRate(
            x.Base.ToUpperInvariant(), x.Quote.ToUpperInvariant(), x.Value, x.Type, false,
            date, url, attribution)).ToList();

        // BoZ currently publishes only a small direct currency table. Complete the
        // maintained FCY basket using AllRatesToday's immutable daily reference
        // table. These rows remain explicitly marked as derived and retain their
        // own date/source, so they can never be mistaken for direct BoZ quotes.
        try
        {
            using var referenceResponse = await clients.CreateClient("fcy-provider").GetAsync(ReferenceUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            if (referenceResponse.IsSuccessStatusCode)
            {
                await using var referenceStream = await referenceResponse.Content.ReadAsStreamAsync(ct);
                var reference = await JsonSerializer.DeserializeAsync<ReferenceResponse>(referenceStream, new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
                if (reference is not null && DateOnly.TryParse(reference.RateDate, out var referenceDate))
                    AddReferenceCrossRates(rates, reference, referenceDate);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // A reference-table outage must not suppress the direct official BoZ
            // rates. The next synchronization can fill the derived pairs.
        }

        return new FcyProviderTable(date, payload.Stale, url, attribution, rates);
    }
    public async Task<FcyProviderHealth> TestAsync(CancellationToken ct = default)
    {
        try { var table = await GetLatestAsync(ct); return new(true, $"Bank of Zambia table for {table.RateDate:yyyy-MM-dd} is available with {table.Rates.Count} rates.", DateTime.UtcNow, table.RateDate, table.Stale); }
        catch (Exception ex) { return new(false, ex.Message, DateTime.UtcNow); }
    }
    private static int Rank(string type) => type.Equals("middle", StringComparison.OrdinalIgnoreCase) ? 0 : type.Equals("buy", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    private sealed record BozResponse([property:JsonPropertyName("bank")] string Bank, [property:JsonPropertyName("rate_date")] string RateDate,
        [property:JsonPropertyName("stale")] bool Stale, [property:JsonPropertyName("rates")] List<BozRate>? Rates,
        [property:JsonPropertyName("attribution")] Attribution? Attribution);
    private sealed record BozRate([property:JsonPropertyName("base")] string Base, [property:JsonPropertyName("quote")] string Quote,
        [property:JsonPropertyName("type")] string Type, [property:JsonPropertyName("value")] decimal Value);
    private sealed record Attribution([property:JsonPropertyName("source")] string Source, [property:JsonPropertyName("url")] string Url);

    private static void AddReferenceCrossRates(List<FcyProviderRate> destination, ReferenceResponse reference, DateOnly date)
    {
        if (!string.Equals(reference.Base, "USD", StringComparison.OrdinalIgnoreCase) || reference.Rates is null) return;
        var usable = reference.Rates.Where(x => x.Value > 0 && x.Quote?.Length == 3)
            .GroupBy(x => x.Quote.ToUpperInvariant()).ToDictionary(x => x.Key, x => x.First().Value);
        if (!usable.TryGetValue("ZMW", out var usdToZmw) || usdToZmw <= 0) return;
        var existing = destination.Select(x => $"{x.FromCurrencyCode}:{x.ToCurrencyCode}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var attribution = reference.Attribution?.Source ?? "AllRatesToday Daily Reference Rate";
        foreach (var (currency, usdToCurrency) in usable)
        {
            if (currency is "USD" or "ZMW" || existing.Contains($"{currency}:ZMW")) continue;
            destination.Add(new FcyProviderRate(currency, "ZMW", usdToZmw / usdToCurrency,
                "daily-reference-cross", true, date, ReferenceUrl, attribution));
        }
    }

    private sealed record ReferenceResponse(
        [property:JsonPropertyName("rate_date")] string RateDate,
        [property:JsonPropertyName("base")] string Base,
        [property:JsonPropertyName("rates")] List<ReferenceRate>? Rates,
        [property:JsonPropertyName("attribution")] Attribution? Attribution);
    private sealed record ReferenceRate([property:JsonPropertyName("quote")] string Quote, [property:JsonPropertyName("value")] decimal Value);
}
