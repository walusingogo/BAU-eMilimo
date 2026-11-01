using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Infrastructure.Data;
using eMilimo.API.Services;
using eMilimo.Core.Services;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/role-permissions"), Authorize]
public class RolePermissionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRolePermissionService _permissions;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemLogWriter _logs;
    private static readonly object[] Processes =
    {
        new { Key="requisitions.view", Name="View Requisitions", Module="Requisitions" }, new { Key="requisitions.initiate", Name="Requisition Initiator", Module="Requisitions" },
        
        new { Key="contributions.view", Name="View Contributions", Module="Contributions" }, new { Key="contributions.initiate", Name="Contribution Initiator", Module="Contributions" },
        new { Key="groups.manage", Name="Groups Management", Module="Administration" },
        new { Key="sections.manage", Name="Sections Management", Module="Administration" }, new { Key="committees.manage", Name="Committees Management", Module="Administration" },
        new { Key="users.manage", Name="User Management", Module="Administration" },
        new { Key="members.view", Name="View Member Directory", Module="Membership" },
        new { Key="members.manage", Name="Maintain Member Directory", Module="Membership" },
        new { Key="members.financial-activity", Name="View Member Financial Activity", Module="Membership" },
        new { Key="membership.sync.manage", Name="Manage Membership Integration", Module="Membership" },
        new { Key="service-collections.view", Name="View Service Collections", Module="Service Collections" },
        new { Key="service-collections.create", Name="Create Service Collections", Module="Service Collections" },
        new { Key="service-collections.edit", Name="Edit and Archive Service Collections", Module="Service Collections" },
        new { Key="service-collections.submit", Name="Submit Service Collections", Module="Service Collections" },
        new { Key="reports.view", Name="Reports", Module="Reports" },
        new { Key="assets.view", Name="View Asset Register", Module="Assets & Fleet" },
        new { Key="assets.request", Name="Request Asset Checkout", Module="Assets & Fleet" },
        new { Key="assets.manage", Name="Register and Maintain Assets", Module="Assets & Fleet" },
        new { Key="assets.approve", Name="Review Asset Checkout Requests", Module="Assets & Fleet" },
        new { Key="assets.reports", Name="Asset and Fleet Reports", Module="Assets & Fleet" }
        ,new { Key="settings.manage", Name="System Settings", Module="Administration" }
        ,new { Key="payments.developer.manage", Name="Mobile Money Developer Mode", Module="Administration" }
        ,new { Key="frb.view", Name="View Financial Record Books", Module="Financial Record Book" }
        ,new { Key="frb.export", Name="Export Financial Record Books", Module="Financial Record Book" }
        ,new { Key="frb.rules.manage", Name="Maintain FRB Assessment Rules", Module="Financial Record Book" }
        ,new { Key="accounting.integration.view", Name="View Accounting Integrations", Module="Accounting Integration" }
        ,new { Key="accounting.integration.manage", Name="Maintain Accounting Profiles and Mappings", Module="Accounting Integration" }
        ,new { Key="accounting.batch.prepare", Name="Prepare Accounting Batches", Module="Accounting Integration" }
        ,new { Key="accounting.batch.approve", Name="Approve Accounting Batches", Module="Accounting Integration" }
        ,new { Key="accounting.batch.export", Name="Export Accounting Batches", Module="Accounting Integration" }
    };

    public RolePermissionsController(AppDbContext context, IRolePermissionService permissions,
        ICurrentUserService currentUser, ISystemLogWriter logs) =>
        (_context, _permissions, _currentUser, _logs) = (context, permissions, currentUser, logs);

    [HttpGet, Authorize(Roles="SuperAdmin,Admin")]
    public async Task<IActionResult> GetMatrix()
    {
        var stored = await _context.RolePermissions.ToDictionaryAsync(x => x.Role, x => x.PermissionsJson);
        var configurable = Processes.Select(p => (string)p.GetType().GetProperty("Key")!.GetValue(p)!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = AuthorityRoles.All
            .Select(role => new { role, permissions = (stored.TryGetValue(role, out var json) ? Read(json) : Defaults(role)).Where(configurable.Contains).ToArray() });
        return Ok(new
        {
            processes = Processes,
            roles,
            note = "Role permissions control baseline module access. Workflow actions are granted by effective office appointments and capabilities."
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromServices] IRolePermissionService permissions, [FromServices] IWorkflowAuthorizationService workflow, [FromServices] eMilimo.Core.Services.ICurrentUserService current)
    {
        IEnumerable<string> effective = await permissions.GetAsync(current.GetUserRole());
        var userId = current.GetUserId();
        var capabilities = userId.HasValue ? await workflow.GetCapabilitiesAsync(userId.Value) : [];
        if (userId.HasValue) effective = effective.Concat(capabilities.SelectMany(WorkflowAuthorizationService.PermissionsFor));
        var now = DateTime.UtcNow;
        var isSteward = userId.HasValue && await _context.OfficeAppointments.AsNoTracking().AnyAsync(x =>
            x.UserId == userId.Value && x.IsActive && x.EffectiveFromUtc <= now &&
            (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) && x.Designation.Code == DesignationCodes.Steward);
        return Ok(new { role = current.GetUserRole(), permissions = effective.Distinct().ToArray(), capabilities, isSteward });
    }

    [HttpPut("{role}"), Authorize(Roles="SuperAdmin,Admin")]
    public async Task<IActionResult> Update(string role, PermissionUpdate request)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsed) || !AuthorityRoles.IsDefined(parsed.ToString()))
            return BadRequest(new { Message="Only stable authority roles can be configured. Church offices are managed through designations and office appointments." });
        if (parsed == UserRole.SuperAdmin) return BadRequest(new { Message="Super Admin permissions cannot be restricted." });
        var allowed = Processes.Select(p => (string)p.GetType().GetProperty("Key")!.GetValue(p)!).ToHashSet();
        var permissions = request.Permissions.Where(allowed.Contains).Distinct().Order().ToArray();
        if (parsed == UserRole.Admin)
            permissions = permissions.Where(p => p is not "requisitions.initiate" and not "contributions.initiate").ToArray();
        var config = await _context.RolePermissions.FirstOrDefaultAsync(x => x.Role == parsed.ToString());
        var before = config == null ? Defaults(parsed.ToString()) : Read(config.PermissionsJson);
        if (config == null) { config = new RolePermission { Id=Guid.NewGuid(), Role=parsed.ToString(), CreatedDate=DateTime.UtcNow }; _context.RolePermissions.Add(config); }
        config.PermissionsJson = JsonSerializer.Serialize(permissions); config.UpdatedDate = DateTime.UtcNow; config.Version++;
        await _context.SaveChangesAsync();
        await _permissions.InvalidateAsync(parsed.ToString());
        await _logs.WriteAsync(new SystemLog
        {
            Level = "Information", Category = "Security", Process = "RolePermissions", Action = "Updated",
            UserId = _currentUser.GetUserId(),
            Message = $"Permissions for stable authority role {parsed} changed from {JsonSerializer.Serialize(before)} to {JsonSerializer.Serialize(permissions)}."
        });
        return Ok(new { role=parsed.ToString(), permissions });
    }

    private static string[] Read(string json) { try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch { return []; } }
    private static string[] Defaults(string role) => role switch
    {
        _ => RolePermissionDefaults.For(role)
    };

    public record PermissionUpdate(string[] Permissions);
}
