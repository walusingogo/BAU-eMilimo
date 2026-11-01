using System.Text.Json;
using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/membership-integration"), Authorize]
public sealed class MembershipIntegrationController(
    AppDbContext db, IEnumerable<IMembershipDirectoryProvider> providers,
    ICurrentUserService current, ISystemLogWriter logs, IDataProtectionProvider protection) : ControllerBase
{
    private readonly IDataProtector _protector = protection.CreateProtector("eMilimo.MembershipDirectory.Secret.v1");

    [HttpGet("settings"), RequirePermission("membership.sync.manage")]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var hasApiKey = await db.IntegrationSettings.AsNoTracking()
            .AnyAsync(x => x.IntegrationKey == "membership-rest-api" && x.ProtectedSettings != string.Empty, ct);
        var items = await db.MembershipIntegrationConfigurations.AsNoTracking().OrderBy(x => x.ProviderCode).ToListAsync(ct);
        return Ok(items.Select(x => View(x, x.ProviderCode == "REST_API" && hasApiKey)));
    }

    [HttpPut("settings/{id:guid}"), RequirePermission("membership.sync.manage")]
    public async Task<IActionResult> Update(Guid id, SaveMembershipIntegrationRequest request, CancellationToken ct)
    {
        var item = await db.MembershipIntegrationConfigurations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { message = "Membership integration configuration not found." });
        if (!request.ProviderCode.Equals(item.ProviderCode, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "The provider code cannot be changed." });
        if (!request.Version.HasValue || request.Version != item.Version) return Conflict(new { message = "The integration settings changed. Reload and try again." });
        if (request.TimeoutSeconds is < 1 or > 300) return BadRequest(new { message = "Timeout must be between 1 and 300 seconds." });
        if (item.ProviderCode == "REST_API" && request.IsEnabled &&
            (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("http" or "https")))
            return BadRequest(new { message = "A valid HTTP or HTTPS endpoint is required before enabling the REST API provider." });
        var before = View(item); item.Endpoint = Clean(request.Endpoint); item.IsEnabled = request.IsEnabled;
        item.UseLocalFallback = request.UseLocalFallback; item.TimeoutSeconds = request.TimeoutSeconds;
        item.FieldOwnershipMode = request.FieldOwnershipMode.Trim().ToUpperInvariant(); item.UpdatedById = current.GetUserId(); item.UpdatedDate = DateTime.UtcNow; item.Version++;
        if (request.IsEnabled)
            await db.MembershipIntegrationConfigurations.Where(x => x.Id != item.Id && x.IsEnabled)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsEnabled, false)
                    .SetProperty(x => x.UpdatedDate, DateTime.UtcNow)
                    .SetProperty(x => x.Version, x => x.Version + 1), ct);
        if (item.ProviderCode == "REST_API" && !string.IsNullOrWhiteSpace(request.ApiKey))
        {
            var secret = await db.IntegrationSettings.FirstOrDefaultAsync(x => x.IntegrationKey == "membership-rest-api", ct);
            if (secret is null)
                db.IntegrationSettings.Add(new IntegrationSetting { Id = Guid.NewGuid(), IntegrationKey = "membership-rest-api", ProtectedSettings = _protector.Protect(request.ApiKey.Trim()), CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1 });
            else { secret.ProtectedSettings = _protector.Protect(request.ApiKey.Trim()); secret.UpdatedDate = DateTime.UtcNow; secret.Version++; }
        }
        await db.SaveChangesAsync(ct);
        var hasApiKey = item.ProviderCode == "REST_API" && await db.IntegrationSettings.AsNoTracking().AnyAsync(x => x.IntegrationKey == "membership-rest-api" && x.ProtectedSettings != string.Empty, ct);
        await Audit("MembershipIntegrationUpdated", id, new { Before = before, After = View(item, hasApiKey) }, ct);
        return Ok(View(item, hasApiKey));
    }

    [HttpPost("{providerCode}/test"), RequirePermission("membership.sync.manage")]
    public async Task<IActionResult> Test(string providerCode, CancellationToken ct)
    {
        var provider = providers.FirstOrDefault(x => x.ProviderCode.Equals(providerCode, StringComparison.OrdinalIgnoreCase));
        if (provider == null) return BadRequest(new { message = "The configured provider adapter is not installed." });
        var result = await provider.TestAsync(ct); await Audit("MembershipProviderTested", Guid.Empty, new { providerCode, result.Available, result.Message }, ct); return Ok(result);
    }

    [HttpGet("search"), RequirePermission("members.view")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int limit = 25, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return BadRequest(new { message = "Enter at least two search characters." });
        var config = await db.MembershipIntegrationConfigurations.AsNoTracking()
            .OrderBy(x => x.ProviderCode == "LOCAL" ? 1 : 0).FirstOrDefaultAsync(x => x.IsEnabled, ct);
        var selected = config == null ? providers.First(x => x.ProviderCode == "LOCAL") : providers.FirstOrDefault(x => x.ProviderCode == config.ProviderCode);
        var fallback = providers.First(x => x.ProviderCode == "LOCAL");
        try
        {
            if (selected == null) throw new InvalidOperationException("Provider adapter is not installed.");
            return Ok(new { provider = selected.ProviderCode, fallback = false, items = await selected.SearchAsync(query, limit, ct) });
        }
        catch (Exception ex) when (config?.UseLocalFallback != false)
        {
            await Audit("MembershipProviderFallback", Guid.Empty, new { config?.ProviderCode, Error = ex.Message }, ct);
            return Ok(new { provider = "LOCAL", fallback = true, warning = "The external membership directory is unavailable. Local fallback results are shown.", items = await fallback.SearchAsync(query, limit, ct) });
        }
    }

    private Task Audit(string action, Guid id, object details, CancellationToken ct) => logs.WriteAsync(new SystemLog
    { Category = "Audit", Process = "Membership Integration", Action = action, UserId = current.GetUserId(), CorrelationId = HttpContext.TraceIdentifier,
      Message = $"{action} for {id}.", Details = JsonSerializer.Serialize(details) }, ct);
    private static object View(MembershipIntegrationConfiguration x, bool hasApiKey = false) => new { x.Id, x.ProviderCode, x.Endpoint, x.IsEnabled, x.UseLocalFallback, x.TimeoutSeconds, x.FieldOwnershipMode, x.Version, HasApiKey = hasApiKey };
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
