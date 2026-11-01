using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Authorization;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/frb"), Authorize]
public sealed class FinancialRecordBooksController(
    IFinancialRecordBookService service, ICurrentUserService current, AppDbContext db,
    ILogger<FinancialRecordBooksController> logger) : ControllerBase
{
    private Guid UserId() => current.GetUserId() ?? throw new UnauthorizedAccessException("Sign in to continue.");

    [HttpGet, RequirePermission("frb.view")]
    public Task<object> List([FromQuery] string? status, [FromQuery] string? search, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => service.ListAsync(status, search, from, to, ct);

    [HttpGet("{id:guid}"), RequirePermission("frb.view")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => await service.GetAsync(id, ct) is { } result ? Ok(result) : NotFound(new { message = "Financial Record Book not found." });

    [HttpGet("eligible-contributions"), RequirePermission("frb.create")]
    public async Task<IActionResult> Eligible([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? sectionId,
        [FromQuery] Guid? contributionTypeId, [FromQuery] string? search, CancellationToken ct)
    { try { return Ok(await service.EligibleContributionsAsync(from, to, sectionId, contributionTypeId, search, ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpGet("eligible-service-collections"), RequirePermission("frb.create")]
    public async Task<IActionResult> EligibleServiceCollections([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? sectionId,
        [FromQuery] Guid? contributionTypeId, [FromQuery] string? search, CancellationToken ct)
    { try { return Ok(await service.EligibleServiceCollectionsAsync(from, to, sectionId, contributionTypeId, search, ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpPost("preview"), RequirePermission("frb.create")]
    public async Task<IActionResult> Preview(SaveFinancialRecordBookRequest request, CancellationToken ct)
    { try { return Ok(await service.PreviewAsync(request, ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpPost, RequirePermission("frb.create")]
    public async Task<IActionResult> Create(SaveFinancialRecordBookRequest request, CancellationToken ct)
    { try { var result = await service.CreateAsync(request, UserId(), ct); return Ok(result); } catch (Exception ex) { return Problem(ex); } }

    [HttpPut("{id:guid}"), RequirePermission("frb.edit")]
    public async Task<IActionResult> Update(Guid id, SaveFinancialRecordBookRequest request, CancellationToken ct)
    { try { return Ok(await service.UpdateAsync(id, request, UserId(), ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpPost("{id:guid}/archive"), RequirePermission("frb.edit")]
    public async Task<IActionResult> Archive(Guid id, ArchiveFinancialRecordBookRequest request, CancellationToken ct)
    { try { return Ok(await service.ArchiveAsync(id, request, UserId(), ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpPost("{id:guid}/submit"), RequirePermission("frb.submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    { try { return Ok(await service.SubmitAsync(id, UserId(), ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpPost("{id:guid}/verify"), RequirePermission("frb.verify")]
    public async Task<IActionResult> Verify(Guid id, FrbVerificationRequest request, CancellationToken ct)
    { try { return Ok(await service.VerifyAsync(id, request, UserId(), ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpPost("{id:guid}/finalize"), RequirePermission("frb.finalize")]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct)
    { try { return Ok(await service.FinalizeAsync(id, UserId(), ct)); } catch (Exception ex) { return Problem(ex); } }

    [HttpGet("{id:guid}/audit"), RequirePermission("frb.view")]
    public async Task<IActionResult> Audit(Guid id, CancellationToken ct)
    {
        if (await service.GetAsync(id, ct) == null) return NotFound(new { message = "Financial Record Book not found." });
        var rows = await db.FrbAudits.AsNoTracking().Where(x => x.FinancialRecordBookId == id)
            .OrderByDescending(x => x.PerformedAtUtc).ToListAsync(ct);
        var users = await db.Users.AsNoTracking().Where(x => rows.Select(r => r.PerformedById).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => (x.FirstName + " " + x.LastName).Trim(), ct);
        return Ok(rows.Select(x => new { x.Id, x.Action, Changes = JsonSerializer.Deserialize<JsonElement>(x.ChangesJson),
            x.PerformedById, PerformedBy = x.PerformedById.HasValue && users.TryGetValue(x.PerformedById.Value, out var name) ? name : "System", x.PerformedAtUtc }));
    }

    private ObjectResult Problem(Exception ex)
    {
        if (ex is KeyNotFoundException) return NotFound(new { message = ex.Message });
        if (ex is UnauthorizedAccessException) return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        if (ex is InvalidOperationException) return BadRequest(new { message = ex.Message });
        if (ex is DbUpdateConcurrencyException) return Conflict(new { message = "This FRB was changed by another user. Reload it and try again.", correlationId = HttpContext.TraceIdentifier });
        logger.LogError(ex, "Unexpected FRB operation failure. CorrelationId: {CorrelationId}", HttpContext.TraceIdentifier);
        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "The FRB operation could not be completed.", correlationId = HttpContext.TraceIdentifier });
    }
}
