using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.API.Authorization;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers;

[ApiController]
[Route("api/audits")]
[Authorize]
public class ProcessAuditsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProcessAuditsController(AppDbContext context) => _context = context;

    [HttpGet("contributions/{id:guid}")]
    [RequirePermission("contributions.view")]
    public async Task<IActionResult> GetContributionAudit(Guid id)
    {
        var entries = await _context.ContributionAudits.AsNoTracking()
            .Where(a => a.ContributionId == id)
            .OrderByDescending(a => a.PerformedAtUtc)
            .ToListAsync();

        return Ok(await PresentAsync(entries.Select(a =>
            (a.Id, a.Action, a.ChangesJson, a.PerformedById, a.PerformedAtUtc))));
    }

    [HttpGet("requisitions/{id:guid}")]
    [RequirePermission("requisitions.view")]
    public async Task<IActionResult> GetRequisitionAudit(Guid id)
    {
        var entries = await _context.RequisitionAudits.AsNoTracking()
            .Where(a => a.RequisitionId == id)
            .OrderByDescending(a => a.PerformedAtUtc)
            .ToListAsync();

        return Ok(await PresentAsync(entries.Select(a =>
            (a.Id, a.Action, a.ChangesJson, a.PerformedById, a.PerformedAtUtc))));
    }

    private async Task<object[]> PresentAsync(
        IEnumerable<(Guid Id, string Action, string Changes, Guid? UserId, DateTime At)> source)
    {
        var entries = source.ToList();
        var userIds = entries.Where(e => e.UserId.HasValue).Select(e => e.UserId!.Value).Distinct().ToList();
        var users = await _context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

        return entries.Select(e => (object)new
        {
            e.Id,
            e.Action,
            Changes = JsonSerializer.Deserialize<JsonElement>(e.Changes),
            PerformedById = e.UserId,
            PerformedBy = e.UserId.HasValue && users.TryGetValue(e.UserId.Value, out var name)
                ? name
                : "System",
            PerformedAtUtc = e.At
        }).ToArray();
    }
}
