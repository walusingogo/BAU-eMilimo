using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/financial-settings"), Authorize]
public sealed class FinancialSettingsController(
    AppDbContext db,
    ICashDenominationSettingsService denominations,
    ICurrentUserService current) : ControllerBase
{
    public sealed record SaveCashDenominationsRequest(IReadOnlyList<decimal> Denominations);

    [HttpGet("cash-denominations"), RequirePermission("settings.manage")]
    public async Task<IActionResult> GetCashDenominations(CancellationToken ct)
    {
        var currency = await db.FcyManagementSettings.AsNoTracking()
            .Select(x => x.BaseCurrencyCode)
            .SingleAsync(ct);
        return Ok(new { currencyCode = currency, denominations = await denominations.GetAsync(ct) });
    }

    [HttpPut("cash-denominations"), RequirePermission("settings.manage")]
    public async Task<IActionResult> SaveCashDenominations(SaveCashDenominationsRequest request, CancellationToken ct)
    {
        var actor = current.GetUserId();
        if (!actor.HasValue) return Unauthorized();
        try
        {
            var saved = await denominations.SaveAsync(request.Denominations ?? [], actor.Value, ct);
            var currency = await db.FcyManagementSettings.AsNoTracking().Select(x => x.BaseCurrencyCode).SingleAsync(ct);
            return Ok(new { currencyCode = currency, denominations = saved });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
