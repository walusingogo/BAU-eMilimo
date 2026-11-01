using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/designations"), Authorize]
public sealed class DesignationsController(AppDbContext db, ICurrentUserService currentUser, ISystemLogWriter logs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? role, [FromQuery] bool includeInactive = false)
    {
        var query = db.Designations.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.RequiredRole == role);
        return Ok(await query.OrderBy(x => x.RequiredRole).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Code, x.Name, x.RequiredRole, x.FunctionalJurisdiction, x.FunctionalAreaId,
                FunctionalAreaCode = x.FunctionalArea == null ? null : x.FunctionalArea.Code,
                FunctionalAreaName = x.FunctionalArea == null ? null : x.FunctionalArea.Name,
                x.Description, x.CapabilitiesJson, x.IsSystem, x.IsActive,
                AppointmentCount = x.OfficeAppointments.Count })
            .ToListAsync());
    }

    [HttpPost, RequirePermission("settings.manage")]
    public async Task<IActionResult> Create(DesignationRequest request)
    {
        var validation = Validate(request);
        if (validation != null) return BadRequest(new { Message = validation });
        var code = NormalizeCode(request.Code);
        if (await db.Designations.AnyAsync(x => x.Code == code)) return Conflict(new { Message = "A designation with this code already exists." });
        var area = await ResolveAreaAsync(request.FunctionalAreaId, request.FunctionalJurisdiction);
        if (area == null) return BadRequest(new { Message = "Select an active functional area." });
        var item = new Designation
        {
            Id = Guid.NewGuid(), Code = code, Name = request.Name.Trim(), RequiredRole = Enum.Parse<UserRole>(request.RequiredRole, true).ToString(),
            FunctionalJurisdiction = area.Code, FunctionalAreaId = area.Id, Description = Clean(request.Description),
            CapabilitiesJson = JsonSerializer.Serialize((request.Capabilities ?? []).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray()),
            IsActive = request.IsActive, CreatedById = currentUser.GetUserId(), UpdatedById = currentUser.GetUserId(),
            CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
        };
        db.Designations.Add(item);
        await db.SaveChangesAsync();
        await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Security", Process = "Designations", Action = "Created", UserId = currentUser.GetUserId(), Message = $"Designation {item.Code} ({item.Id}) created with capabilities {item.CapabilitiesJson}." });
        return CreatedAtAction(nameof(List), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}"), RequirePermission("settings.manage")]
    public async Task<IActionResult> Update(Guid id, DesignationRequest request)
    {
        var item = await db.Designations.FindAsync(id);
        if (item == null) return NotFound();
        var validation = Validate(request);
        if (validation != null) return BadRequest(new { Message = validation });
        var code = NormalizeCode(request.Code);
        if (await db.Designations.AnyAsync(x => x.Id != id && x.Code == code)) return Conflict(new { Message = "A designation with this code already exists." });
        if (item.IsSystem && item.Code != code) return BadRequest(new { Message = "System designation codes cannot be changed." });
        var before = new { item.Code, item.Name, item.RequiredRole, item.FunctionalAreaId, item.FunctionalJurisdiction, item.CapabilitiesJson, item.IsActive };
        var requiredRole = Enum.Parse<UserRole>(request.RequiredRole, true).ToString();
        var area = await ResolveAreaAsync(request.FunctionalAreaId, request.FunctionalJurisdiction);
        if (area == null) return BadRequest(new { Message = "Select an active functional area." });
        var jurisdiction = area.Code;
        if (item.IsSystem && (item.RequiredRole != requiredRole || (item.FunctionalAreaId.HasValue && item.FunctionalAreaId != area.Id)))
            return BadRequest(new { Message = "The authority role and functional jurisdiction of a system designation cannot be changed." });
        item.Code = code; item.Name = request.Name.Trim(); item.RequiredRole = requiredRole;
        item.FunctionalJurisdiction = jurisdiction; item.FunctionalAreaId = area.Id; item.Description = Clean(request.Description);
        item.CapabilitiesJson = JsonSerializer.Serialize((request.Capabilities ?? []).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray());
        item.IsActive = request.IsActive; item.UpdatedById = currentUser.GetUserId(); item.UpdatedDate = DateTime.UtcNow; item.Version++;
        await db.SaveChangesAsync();
        await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Security", Process = "Designations", Action = "Updated", UserId = currentUser.GetUserId(), Message = $"Designation {item.Id} changed from {JsonSerializer.Serialize(before)} to {JsonSerializer.Serialize(new { item.Code, item.Name, item.RequiredRole, item.FunctionalAreaId, item.FunctionalJurisdiction, item.CapabilitiesJson, item.IsActive })}." });
        return Ok(item);
    }

    private static string? Validate(DesignationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return "Code and name are required.";
        if (!request.FunctionalAreaId.HasValue && string.IsNullOrWhiteSpace(request.FunctionalJurisdiction))
            return "Select a functional area.";
        if (!Enum.TryParse<UserRole>(request.RequiredRole, true, out var role)) return "Select a valid authority role.";
        if (!AuthorityRoles.IsDesignationAssignable(role.ToString()))
            return "Select an authority role that can hold an office appointment.";
        var unknown = (request.Capabilities ?? []).Except(WorkflowCapabilities.All, StringComparer.OrdinalIgnoreCase).ToArray();
        if (unknown.Length > 0) return $"Unknown workflow capabilities: {string.Join(", ", unknown)}.";
        return null;
    }
    private static string NormalizeCode(string value) => string.Join('_', value.Trim().ToUpperInvariant().Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task<FunctionalArea?> ResolveAreaAsync(Guid? id, string? codeValue)
    {
        if (id.HasValue) return await db.FunctionalAreas.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (string.IsNullOrWhiteSpace(codeValue)) return null;
        var code = NormalizeCode(codeValue);
        return await db.FunctionalAreas.FirstOrDefaultAsync(x => x.Code == code && x.IsActive);
    }
    public sealed record DesignationRequest(string Code, string Name, string RequiredRole, string? FunctionalJurisdiction, string? Description, string[]? Capabilities, bool IsActive = true, Guid? FunctionalAreaId = null);
}
