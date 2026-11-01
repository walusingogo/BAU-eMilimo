using eMilimo.API.Authorization;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/users/{userId:guid}/office-appointments"), Authorize, RequirePermission("users.manage")]
public sealed class OfficeAppointmentsController(AppDbContext db, ICurrentUserService current, ISystemLogWriter logs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid userId, [FromQuery] bool includeEnded = true)
    {
        var query = db.OfficeAppointments.AsNoTracking().Where(x => x.UserId == userId);
        if (!includeEnded) { var now = DateTime.UtcNow; query = query.Where(x => x.IsActive && x.EffectiveFromUtc <= now && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now)); }
        return Ok(await query.OrderByDescending(x => x.EffectiveFromUtc).Select(x => new
        {
            x.Id, x.UserId, x.DesignationId, Designation = x.Designation.Name, DesignationCode = x.Designation.Code,
            x.FunctionalAreaId, FunctionalArea = x.FunctionalArea.Name, FunctionalAreaCode = x.FunctionalArea.Code,
            x.ScopeType, x.SectionId, Section = x.Section == null ? null : x.Section.SectionName,
            x.GroupId, Group = x.Group == null ? null : x.Group.GroupName,
            x.CommitteeId, Committee = x.Committee == null ? null : x.Committee.CommitteeName,
            x.EffectiveFromUtc, x.EffectiveToUtc, x.IsActive, x.AssignedById, x.CreatedDate, x.UpdatedDate, x.Version
        }).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid userId, AppointmentRequest request)
    {
        var user = await db.Users.FindAsync(userId); if (user == null) return NotFound(new { Message = "User was not found." });
        var designation = await db.Designations.FindAsync(request.DesignationId);
        var area = await db.FunctionalAreas.FindAsync(request.FunctionalAreaId);
        if (designation == null || !designation.IsActive) return BadRequest(new { Message = "Select an active designation." });
        if (area == null || !area.IsActive) return BadRequest(new { Message = "Select an active functional area." });
        if (!user.Role.Equals(designation.RequiredRole, StringComparison.OrdinalIgnoreCase)) return BadRequest(new { Message = $"{designation.Name} requires the {designation.RequiredRole} authority role." });
        var scopeError = await ValidateScopeAsync(request.ScopeType, request.SectionId, request.GroupId, request.CommitteeId);
        if (scopeError != null) return BadRequest(new { Message = scopeError });
        var from = request.EffectiveFromUtc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(request.EffectiveFromUtc, DateTimeKind.Utc) : request.EffectiveFromUtc.ToUniversalTime();
        var to = request.EffectiveToUtc?.ToUniversalTime();
        if (to.HasValue && to <= from) return BadRequest(new { Message = "The appointment end must be after its effective start." });
        var normalizedScope = request.ScopeType.ToUpperInvariant();
        var overlaps = await db.OfficeAppointments.AnyAsync(x => x.UserId == userId && x.DesignationId == designation.Id &&
            x.FunctionalAreaId == area.Id && x.ScopeType == normalizedScope && x.SectionId == request.SectionId &&
            x.GroupId == request.GroupId && x.CommitteeId == request.CommitteeId &&
            (!to.HasValue || x.EffectiveFromUtc < to.Value) && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > from));
        if (overlaps) return Conflict(new { Message = "An overlapping appointment already exists for this user, office, functional area, and scope." });
        var actor = current.GetUserId();
        var item = new OfficeAppointment
        {
            Id = Guid.NewGuid(), UserId = userId, DesignationId = designation.Id, FunctionalAreaId = area.Id,
            ScopeType = normalizedScope, SectionId = request.SectionId, GroupId = request.GroupId, CommitteeId = request.CommitteeId,
            EffectiveFromUtc = from, EffectiveToUtc = to, IsActive = !to.HasValue || to > DateTime.UtcNow, AssignedById = actor,
            CreatedById = actor, UpdatedById = actor, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
        };
        db.OfficeAppointments.Add(item); await db.SaveChangesAsync();
        await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Security", Process = "OfficeAppointments", Action = "Assigned",
            UserId = actor, Message = $"Office appointment {item.Id} assigned to user {userId} for {designation.Code}/{area.Code} with {item.ScopeType} scope." });
        return Ok(new { item.Id, Message = "Office appointment created." });
    }

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> End(Guid userId, Guid id, EndAppointmentRequest request)
    {
        var item = await db.OfficeAppointments.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId); if (item == null) return NotFound();
        if (!item.IsActive) return BadRequest(new { Message = "This appointment has already ended." });
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { Message = "A reason is required when ending an office appointment." });
        var end = request.EffectiveToUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        if (end <= item.EffectiveFromUtc) return BadRequest(new { Message = "The appointment end must be after its effective start." });
        item.EffectiveToUtc = end; item.IsActive = false; item.UpdatedById = current.GetUserId(); item.UpdatedDate = DateTime.UtcNow; item.Version++;
        await db.SaveChangesAsync();
        await logs.WriteAsync(new SystemLog { Level = "Information", Category = "Security", Process = "OfficeAppointments", Action = "Ended",
            UserId = current.GetUserId(), Message = $"Office appointment {item.Id} for user {userId} ended at {end:O}. Reason: {request.Reason.Trim()}." });
        return Ok(new { Message = "Office appointment ended without deleting its history." });
    }

    private async Task<string?> ValidateScopeAsync(string scope, Guid? sectionId, Guid? groupId, Guid? committeeId)
    {
        scope = (scope ?? "").ToUpperInvariant();
        if (!AuthorityScopeTypes.All.Contains(scope)) return "Select a valid authority scope.";
        var selected = (sectionId.HasValue ? 1 : 0) + (groupId.HasValue ? 1 : 0) + (committeeId.HasValue ? 1 : 0);
        if ((scope == AuthorityScopeTypes.Congregation || scope == AuthorityScopeTypes.FunctionalArea) && selected != 0) return "This scope must not specify an organizational entity.";
        if (scope == AuthorityScopeTypes.Section && (!sectionId.HasValue || selected != 1)) return "Section scope requires exactly one Section.";
        if (scope == AuthorityScopeTypes.Group && (!groupId.HasValue || selected != 1)) return "Group scope requires exactly one Group.";
        if (scope == AuthorityScopeTypes.Committee && (!committeeId.HasValue || selected != 1)) return "Committee scope requires exactly one Committee.";
        if (scope == AuthorityScopeTypes.Entity && selected != 1) return "Entity scope requires exactly one Section, Group, or Committee.";
        if (sectionId.HasValue && !await db.Sections.AnyAsync(x => x.Id == sectionId && x.IsActive)) return "The selected Section does not exist or is inactive.";
        if (groupId.HasValue && !await db.Groups.AnyAsync(x => x.Id == groupId && x.IsActive && x.GroupType == "ChurchGroup")) return "The selected Group does not exist or is inactive.";
        if (committeeId.HasValue && !await db.Committees.AnyAsync(x => x.Id == committeeId && x.IsActive)) return "The selected Committee does not exist or is inactive.";
        return null;
    }

    public sealed record AppointmentRequest(Guid DesignationId, Guid FunctionalAreaId, string ScopeType, Guid? SectionId,
        Guid? GroupId, Guid? CommitteeId, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc);
    public sealed record EndAppointmentRequest(DateTime? EffectiveToUtc, string? Reason);
}
