using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/functional-areas"), Authorize]
public sealed class FunctionalAreasController(AppDbContext db, ICurrentUserService current, ISystemLogWriter logs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false)
    {
        var query = db.FunctionalAreas.AsNoTracking();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return Ok(await query.OrderBy(x => x.Name).Select(x => new
        {
            x.Id, x.Code, x.Name, x.Description, x.IsSystem, x.IsActive,
            AppointmentCount = x.OfficeAppointments.Count
        }).ToListAsync());
    }

    [HttpPost, RequirePermission("settings.manage")]
    public async Task<IActionResult> Create(FunctionalAreaRequest request)
    {
        var code = Normalize(request.Code);
        if (code.Length == 0 || string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { Message = "Code and name are required." });
        if (await db.FunctionalAreas.AnyAsync(x => x.Code == code)) return Conflict(new { Message = "A functional area with this code already exists." });
        var actor = current.GetUserId();
        var item = new FunctionalArea { Id = Guid.NewGuid(), Code = code, Name = request.Name.Trim(), Description = Clean(request.Description),
            IsActive = true, IsSystem = false, CreatedById = actor, UpdatedById = actor };
        db.FunctionalAreas.Add(item); await db.SaveChangesAsync();
        await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Security", Process = "FunctionalAreas", Action = "Created", UserId = actor, Message = $"Functional area {item.Code} ({item.Id}) created." });
        return Ok(item);
    }

    [HttpPut("{id:guid}"), RequirePermission("settings.manage")]
    public async Task<IActionResult> Update(Guid id, FunctionalAreaRequest request)
    {
        var item = await db.FunctionalAreas.FindAsync(id); if (item == null) return NotFound();
        var code = Normalize(request.Code);
        if (code.Length == 0 || string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { Message = "Code and name are required." });
        if (item.IsSystem && item.Code != code) return BadRequest(new { Message = "System functional-area codes cannot be changed." });
        if (await db.FunctionalAreas.AnyAsync(x => x.Id != id && x.Code == code)) return Conflict(new { Message = "A functional area with this code already exists." });
        var before = new { item.Code, item.Name, item.Description, item.IsActive };
        item.Code = code; item.Name = request.Name.Trim(); item.Description = Clean(request.Description); item.IsActive = request.IsActive;
        item.UpdatedById = current.GetUserId(); item.UpdatedDate = DateTime.UtcNow; item.Version++;
        await db.SaveChangesAsync();
        await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Security", Process = "FunctionalAreas", Action = "Updated", UserId = current.GetUserId(), Message = $"Functional area {item.Id} changed from {System.Text.Json.JsonSerializer.Serialize(before)} to {System.Text.Json.JsonSerializer.Serialize(new { item.Code, item.Name, item.Description, item.IsActive })}." });
        return Ok(item);
    }

    private static string Normalize(string value) => string.Join('_', (value ?? "").Trim().ToUpperInvariant().Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public sealed record FunctionalAreaRequest(string Code, string Name, string? Description, bool IsActive = true);
}
