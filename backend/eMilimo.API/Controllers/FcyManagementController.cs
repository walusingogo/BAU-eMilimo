using System.Text.Json;
using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/fcy-management"), Authorize]
public sealed class FcyManagementController(
    AppDbContext db,
    ICurrentUserService current,
    ISystemLogWriter logs,
    IEnumerable<IFcyRateProvider> providers,
    IFcyProviderSettings providerSettings) : ControllerBase
{
    private static readonly string[] SupportedModes = ["MANUAL_ONLY", "PROVIDER_WITH_MANUAL_FALLBACK"];
    private Guid UserId() => current.GetUserId() ?? throw new UnauthorizedAccessException();

    [HttpGet("settings"), RequirePermission("settings.manage")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var settings = await db.FcyManagementSettings.AsNoTracking().SingleAsync(ct);
        return Ok(settings);
    }

    [HttpPut("settings"), RequirePermission("settings.manage")]
    public async Task<IActionResult> UpdateSettings(SaveFcyManagementSettingRequest request, CancellationToken ct)
    {
        var baseCode = NormalizeCode(request.BaseCurrencyCode);
        var mode = request.RateSourceMode.Trim().ToUpperInvariant();
        if (!SupportedModes.Contains(mode))
            return BadRequest(new { message = "Rate source mode must be MANUAL_ONLY or PROVIDER_WITH_MANUAL_FALLBACK." });
        if (request.MaximumRateAgeHours is < 1 or > 8760)
            return BadRequest(new { message = "Maximum rate age must be between 1 hour and 365 days." });
        if (request.OverrideTolerancePercent is < 0 or > 100)
            return BadRequest(new { message = "Override tolerance must be between 0 and 100 percent." });
        if (mode == "PROVIDER_WITH_MANUAL_FALLBACK" && string.IsNullOrWhiteSpace(request.ProviderCode))
            return BadRequest(new { message = "Select or enter a provider when provider conversion mode is enabled." });
        if (!await db.Currencies.AnyAsync(x => x.Code == baseCode && x.IsActive, ct))
            return BadRequest(new { message = "The base currency must be an active maintained currency." });

        var settings = await db.FcyManagementSettings.SingleAsync(ct);
        if (settings.Version != request.Version)
            return Conflict(new { message = "FCY settings changed while you were editing them. Reload and try again." });

        var before = SettingsAuditView(settings);
        settings.IsEnabled = request.IsEnabled;
        settings.BaseCurrencyCode = baseCode;
        settings.RateSourceMode = mode;
        settings.ProviderCode = NullIfWhiteSpace(request.ProviderCode);
        settings.AllowManualRateOverride = request.AllowManualRateOverride;
        settings.RequireApprovalForManualOverride = request.AllowManualRateOverride && request.RequireApprovalForManualOverride;
        settings.AllowLastKnownRateFallback = request.AllowLastKnownRateFallback;
        settings.MaximumRateAgeHours = request.MaximumRateAgeHours;
        settings.OverrideTolerancePercent = request.OverrideTolerancePercent;
        settings.UpdatedById = UserId();
        settings.UpdatedDate = DateTime.UtcNow;
        settings.Version++;
        var currencies = await db.Currencies.ToListAsync(ct);
        foreach (var currency in currencies.Where(x => x.IsBaseCurrency != (x.Code == baseCode)))
        {
            currency.IsBaseCurrency = currency.Code == baseCode;
            currency.UpdatedById = UserId();
            currency.UpdatedDate = DateTime.UtcNow;
            currency.Version++;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "FCY settings changed while you were editing them. Reload and try again." });
        }

        await AuditAsync("FcySettingsUpdated", settings.Id, new { Before = before, After = SettingsAuditView(settings) }, ct);
        return Ok(settings);
    }

    [HttpGet("currencies"), RequirePermission("settings.manage")]
    public async Task<IActionResult> GetCurrencies(CancellationToken ct) => Ok(await db.Currencies.AsNoTracking()
        .OrderByDescending(x => x.IsBaseCurrency).ThenBy(x => x.Code).ToListAsync(ct));

    [HttpPost("currencies"), RequirePermission("settings.manage")]
    public async Task<IActionResult> CreateCurrency(SaveCurrencyRequest request, CancellationToken ct)
    {
        var code = NormalizeCode(request.Code);
        var validation = ValidateCurrency(request, code);
        if (validation != null) return BadRequest(new { message = validation });
        if (await db.Currencies.AnyAsync(x => x.Code == code, ct))
            return Conflict(new { message = "That currency code already exists." });

        var actor = UserId();
        var currency = new Currency
        {
            Id = Guid.NewGuid(), Code = code, Name = request.Name.Trim(), Symbol = request.Symbol.Trim(),
            DecimalPlaces = request.DecimalPlaces, IsActive = request.IsActive,
            CreatedById = actor, UpdatedById = actor
        };
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(ct);
        await AuditAsync("CurrencyCreated", currency.Id, new { After = CurrencyAuditView(currency) }, ct);
        return CreatedAtAction(nameof(GetCurrencies), new { currency.Id }, currency);
    }

    [HttpPut("currencies/{id:guid}"), RequirePermission("settings.manage")]
    public async Task<IActionResult> UpdateCurrency(Guid id, SaveCurrencyRequest request, CancellationToken ct)
    {
        var currency = await db.Currencies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (currency == null) return NotFound(new { message = "Currency not found." });
        var code = NormalizeCode(request.Code);
        var validation = ValidateCurrency(request, code);
        if (validation != null) return BadRequest(new { message = validation });
        if (!string.Equals(currency.Code, code, StringComparison.Ordinal))
            return BadRequest(new { message = "A currency code is immutable after creation. Create a new currency instead." });
        if (request.Version != currency.Version)
            return Conflict(new { message = "This currency changed while you were editing it. Reload and try again." });
        if (!request.IsActive && currency.IsBaseCurrency)
            return BadRequest(new { message = "The base currency cannot be deactivated." });

        var before = CurrencyAuditView(currency);
        currency.Name = request.Name.Trim();
        currency.Symbol = request.Symbol.Trim();
        currency.DecimalPlaces = request.DecimalPlaces;
        currency.IsActive = request.IsActive;
        currency.UpdatedById = UserId();
        currency.UpdatedDate = DateTime.UtcNow;
        currency.Version++;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This currency changed while you were editing it. Reload and try again." });
        }
        await AuditAsync("CurrencyUpdated", currency.Id, new { Before = before, After = CurrencyAuditView(currency) }, ct);
        return Ok(currency);
    }

    [HttpGet("rates"), RequirePermission("settings.manage")]
    public async Task<IActionResult> GetRates([FromQuery] int limit = 100, CancellationToken ct = default) =>
        Ok(await db.ExchangeRates.AsNoTracking().OrderByDescending(x => x.EffectiveAtUtc)
            .Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpPost("rates"), RequirePermission("settings.manage")]
    public async Task<IActionResult> CreateManualRate(SaveExchangeRateRequest request, CancellationToken ct)
    {
        var fromCode = NormalizeCode(request.FromCurrencyCode);
        var toCode = NormalizeCode(request.ToCurrencyCode);
        if (fromCode == toCode) return BadRequest(new { message = "Source and target currencies must be different." });
        if (request.Rate <= 0) return BadRequest(new { message = "Exchange rate must be greater than zero." });
        if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc <= request.EffectiveAtUtc)
            return BadRequest(new { message = "Expiry must be later than the effective time." });
        if (string.IsNullOrWhiteSpace(request.SourceReference))
            return BadRequest(new { message = "A rate source reference is required." });
        if (await db.Currencies.CountAsync(x => (x.Code == fromCode || x.Code == toCode) && x.IsActive, ct) != 2)
            return BadRequest(new { message = "Both currencies must be active maintained currencies." });
        if (await db.ExchangeRates.AnyAsync(x => x.FromCurrencyCode == fromCode && x.ToCurrencyCode == toCode && x.EffectiveAtUtc == request.EffectiveAtUtc, ct))
            return Conflict(new { message = "A rate for this currency pair already starts at that time." });

        var actor = UserId();
        var rate = new ExchangeRate
        {
            Id = Guid.NewGuid(), FromCurrencyCode = fromCode, ToCurrencyCode = toCode, Rate = request.Rate,
            EffectiveAtUtc = request.EffectiveAtUtc.ToUniversalTime(), ExpiresAtUtc = request.ExpiresAtUtc?.ToUniversalTime(),
            SourceKind = "MANUAL", SourceReference = request.SourceReference.Trim(), Reason = NullIfWhiteSpace(request.Reason),
            IsApproved = true, ApprovedById = actor, ApprovedAtUtc = DateTime.UtcNow,
            CreatedById = actor, UpdatedById = actor
        };
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(ct);
        await AuditAsync("ManualExchangeRateCreated", rate.Id, new
        {
            rate.FromCurrencyCode, rate.ToCurrencyCode, rate.Rate, rate.EffectiveAtUtc,
            rate.ExpiresAtUtc, rate.SourceReference, rate.Reason
        }, ct);
        return Ok(rate);
    }

    [HttpGet("provider-settings"), RequirePermission("settings.manage")]
    public async Task<IActionResult> GetProviderSettings(CancellationToken ct)
    {
        var value = await providerSettings.ViewAsync(ct);
        return Ok(new { ProviderCode = AllRatesTodayBozProvider.Code, value.HasApiKey, value.UseOpenEndpoint,
            KeyedEndpoint = "https://allratestoday.com/api/v1/central-bank/boz/latest",
            OpenEndpoint = "https://allratestoday.com/api/open/central-bank/boz" });
    }

    [HttpPut("provider-settings"), RequirePermission("settings.manage")]
    public async Task<IActionResult> SaveProviderSettings(SaveFcyProviderSettingsRequest request, CancellationToken ct)
    {
        await providerSettings.SaveAsync(request.ApiKey, request.UseOpenEndpoint, ct);
        await AuditAsync("FcyProviderSettingsUpdated", Guid.Empty, new { ProviderCode = AllRatesTodayBozProvider.Code,
            HasApiKey = !string.IsNullOrWhiteSpace(request.ApiKey), request.UseOpenEndpoint }, ct);
        return await GetProviderSettings(ct);
    }

    [HttpPost("provider/test"), RequirePermission("settings.manage")]
    public async Task<IActionResult> TestProvider(CancellationToken ct)
    {
        var provider = providers.First(x => x.ProviderCode == AllRatesTodayBozProvider.Code);
        var result = await provider.TestAsync(ct);
        await AuditAsync("FcyProviderTested", Guid.Empty, new { provider.ProviderCode, result.Available, result.Message, result.RateDate, result.Stale }, ct);
        return result.Available ? Ok(result) : StatusCode(StatusCodes.Status502BadGateway, result);
    }

    [HttpPost("provider/sync-latest"), RequirePermission("settings.manage")]
    public async Task<IActionResult> SyncLatest(CancellationToken ct)
    {
        var settings = await db.FcyManagementSettings.AsNoTracking().SingleAsync(ct);
        if (!settings.IsEnabled || settings.RateSourceMode != "PROVIDER_WITH_MANUAL_FALLBACK" || settings.ProviderCode != AllRatesTodayBozProvider.Code)
            return BadRequest(new { message = "Enable FCY provider mode with the AllRatesToday BoZ provider before synchronizing." });
        var provider = providers.First(x => x.ProviderCode == AllRatesTodayBozProvider.Code);
        FcyProviderTable table;
        try { table = await provider.GetLatestAsync(ct); }
        catch (Exception ex) { return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message }); }
        if (table.Stale && !settings.AllowLastKnownRateFallback)
            return BadRequest(new { message = "The provider marked its latest BoZ table as stale and last-known-rate fallback is disabled." });
        var active = await db.Currencies.AsNoTracking().Where(x => x.IsActive).Select(x => x.Code).ToListAsync(ct);
        var imported = 0; var updated = 0; var skipped = 0; var actor = UserId();
        foreach (var input in table.Rates.Where(x => x.ToCurrencyCode == settings.BaseCurrencyCode && active.Contains(x.FromCurrencyCode)))
        {
            var rateDate = input.RateDate ?? table.RateDate;
            var effective = DateTime.SpecifyKind(rateDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var sourceReference = input.SourceReference ?? table.SourceReference;
            var attribution = input.Attribution ?? table.Attribution;
            var existing = await db.ExchangeRates.FirstOrDefaultAsync(x => x.FromCurrencyCode == input.FromCurrencyCode &&
                x.ToCurrencyCode == input.ToCurrencyCode && x.EffectiveAtUtc == effective, ct);
            if (existing != null && existing.SourceKind != "PROVIDER") { skipped++; continue; }
            if (existing == null)
            {
                db.ExchangeRates.Add(new ExchangeRate { Id = Guid.NewGuid(), FromCurrencyCode = input.FromCurrencyCode,
                    ToCurrencyCode = input.ToCurrencyCode, Rate = input.Rate, EffectiveAtUtc = effective,
                    ExpiresAtUtc = effective.AddHours(settings.MaximumRateAgeHours), SourceKind = "PROVIDER",
                    SourceReference = sourceReference, Reason = $"{attribution}; {input.RateType} rate; derived={input.Derived}",
                    IsApproved = true, ApprovedById = actor, ApprovedAtUtc = DateTime.UtcNow, CreatedById = actor, UpdatedById = actor });
                imported++;
            }
            else
            {
                existing.Rate = input.Rate; existing.ExpiresAtUtc = effective.AddHours(settings.MaximumRateAgeHours);
                existing.SourceReference = sourceReference; existing.Reason = $"{attribution}; {input.RateType} rate; derived={input.Derived}";
                existing.UpdatedById = actor; existing.UpdatedDate = DateTime.UtcNow; existing.Version++; updated++;
            }
        }
        await db.SaveChangesAsync(ct);
        var synchronizedPairs = table.Rates.Where(x => x.ToCurrencyCode == settings.BaseCurrencyCode && active.Contains(x.FromCurrencyCode))
            .Select(x => new { From = x.FromCurrencyCode, To = x.ToCurrencyCode, x.Derived, RateDate = x.RateDate ?? table.RateDate }).ToArray();
        await AuditAsync("FcyProviderRatesSynchronized", Guid.Empty, new { provider.ProviderCode, table.RateDate, table.Stale, imported, updated, skipped, synchronizedPairs }, ct);
        return Ok(new { provider = provider.ProviderCode, table.RateDate, table.Stale, imported, updated, skipped, pairs = synchronizedPairs, table.Attribution });
    }

    private static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ValidateCurrency(SaveCurrencyRequest request, string code)
    {
        if (code.Length != 3 || code.Any(x => !char.IsAsciiLetter(x))) return "Currency code must contain exactly three letters.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Currency name is required.";
        if (request.Name.Trim().Length > 100) return "Currency name cannot exceed 100 characters.";
        if (string.IsNullOrWhiteSpace(request.Symbol) || request.Symbol.Trim().Length > 10) return "Currency symbol is required and cannot exceed 10 characters.";
        if (request.DecimalPlaces is < 0 or > 6) return "Decimal places must be between 0 and 6.";
        return null;
    }

    private static object SettingsAuditView(FcyManagementSetting x) => new
    {
        x.IsEnabled, x.BaseCurrencyCode, x.RateSourceMode, x.ProviderCode, x.AllowManualRateOverride,
        x.RequireApprovalForManualOverride, x.AllowLastKnownRateFallback, x.MaximumRateAgeHours, x.OverrideTolerancePercent, x.Version
    };

    private static object CurrencyAuditView(Currency x) => new
        { x.Code, x.Name, x.Symbol, x.DecimalPlaces, x.IsBaseCurrency, x.IsActive, x.Version };

    private Task AuditAsync(string action, Guid entityId, object details, CancellationToken ct) => logs.WriteAsync(new SystemLog
    {
        Level = "Information", Category = "Audit", Process = "FCY Management", Action = action,
        UserId = UserId(), CorrelationId = HttpContext.TraceIdentifier,
        Message = $"{action} for {entityId}.", Details = JsonSerializer.Serialize(details)
    }, ct);
}
