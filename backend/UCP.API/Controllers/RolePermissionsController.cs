using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;
using UCP.API.Services;

namespace UCP.API.Controllers;

[ApiController, Route("api/role-permissions"), Authorize]
public class RolePermissionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private const string Prefix = "RolePermissions:";
    private static readonly object[] Processes =
    {
        new { Key="requisitions.view", Name="View Requisitions", Module="Requisitions" }, new { Key="requisitions.initiate", Name="Requisition Initiator", Module="Requisitions" },
        new { Key="requisitions.approve", Name="Requisition Approver", Module="Requisitions" }, new { Key="requisitions.disburse", Name="Requisition Disbursement", Module="Requisitions" },
        new { Key="contributions.view", Name="View Contributions", Module="Contributions" }, new { Key="contributions.initiate", Name="Contribution Initiator", Module="Contributions" },
        new { Key="contributions.confirm", Name="Contribution Confirmation", Module="Contributions" }, new { Key="groups.manage", Name="Groups Management", Module="Administration" },
        new { Key="sections.manage", Name="Sections Management", Module="Administration" }, new { Key="committees.manage", Name="Committees Management", Module="Administration" },
        new { Key="users.manage", Name="User Management", Module="Administration" }, new { Key="reports.view", Name="Reports", Module="Reports" }
    };

    public RolePermissionsController(AppDbContext context) => _context = context;

    [HttpGet, Authorize(Roles="SuperAdmin")]
    public async Task<IActionResult> GetMatrix()
    {
        var stored = await _context.SystemConfigs.Where(x => x.ConfigKey.StartsWith(Prefix) && !x.IsArchived).ToDictionaryAsync(x => x.ConfigKey, x => x.ConfigValue);
        var roles = Enum.GetNames<UserRole>().Select(role => new { role, permissions = stored.TryGetValue(Prefix + role, out var json) ? Read(json) : Defaults(role) });
        return Ok(new { processes = Processes, roles });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromServices] IRolePermissionService permissions, [FromServices] UCP.Core.Services.ICurrentUserService current)
        => Ok(new { role=current.GetUserRole(), permissions=await permissions.GetAsync(current.GetUserRole()) });

    [HttpPut("{role}"), Authorize(Roles="SuperAdmin")]
    public async Task<IActionResult> Update(string role, PermissionUpdate request)
    {
        if (!Enum.TryParse<UserRole>(role, true, out var parsed)) return BadRequest(new { Message="Unknown role." });
        if (parsed == UserRole.SuperAdmin) return BadRequest(new { Message="Super Admin permissions cannot be restricted." });
        var allowed = Processes.Select(p => (string)p.GetType().GetProperty("Key")!.GetValue(p)!).ToHashSet();
        var permissions = request.Permissions.Where(allowed.Contains).Distinct().Order().ToArray();
        var key = Prefix + parsed;
        var config = await _context.SystemConfigs.FirstOrDefaultAsync(x => x.ConfigKey == key);
        if (config == null) { config = new SystemConfig { Id=Guid.NewGuid(), ConfigKey=key, Description=$"Process permissions assigned to {parsed}" }; _context.SystemConfigs.Add(config); }
        config.ConfigValue = JsonSerializer.Serialize(permissions); config.UpdatedDate = DateTime.UtcNow; config.Version++;
        await _context.SaveChangesAsync();
        return Ok(new { role=parsed.ToString(), permissions });
    }

    private static string[] Read(string json) { try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch { return []; } }
    private static string[] Defaults(string role) => role switch
    {
        _ => RolePermissionDefaults.For(role)
    };

    public record PermissionUpdate(string[] Permissions);
}
