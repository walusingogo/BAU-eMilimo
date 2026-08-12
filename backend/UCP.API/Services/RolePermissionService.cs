using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

public interface IRolePermissionService { Task<bool> HasAsync(string? role,string permission); Task<string[]> GetAsync(string? role); }
public class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _context;
    public RolePermissionService(AppDbContext context)=>_context=context;
    public async Task<bool> HasAsync(string? role,string permission)=>role==UserRole.SuperAdmin.ToString()||(await GetAsync(role)).Contains(permission);
    public async Task<string[]> GetAsync(string? role)
    {
        if(string.IsNullOrWhiteSpace(role))return [];
        if(role==UserRole.SuperAdmin.ToString())return RolePermissionDefaults.All;
        var json=await _context.RolePermissions.Where(x=>x.Role==role).Select(x=>x.PermissionsJson).FirstOrDefaultAsync();
        if(json!=null)try
        {
            var stored=JsonSerializer.Deserialize<string[]>(json)??[];
            return role==UserRole.Admin.ToString()?stored.Where(p=>p is not "requisitions.initiate" and not "contributions.initiate").ToArray():stored;
        }catch(JsonException){}
        return RolePermissionDefaults.For(role);
    }
}
public static class RolePermissionDefaults
{
    public static readonly string[] All=["requisitions.view","requisitions.initiate","requisitions.approve","requisitions.disburse","contributions.view","contributions.initiate","contributions.confirm","groups.manage","sections.manage","committees.manage","users.manage","reports.view","assets.view","assets.request","assets.manage","assets.approve","assets.reports","settings.manage"];
    public static string[] For(string role)=>role switch
    {
        "SuperAdmin"=>All,
        "Admin"=>All.Where(p=>p is not "requisitions.initiate" and not "contributions.initiate").ToArray(),
        "Auditor"=>["requisitions.view","contributions.view","reports.view","assets.view","assets.reports"],
        "Accountant"=>["requisitions.view","requisitions.disburse","contributions.view","contributions.confirm","reports.view","assets.view","assets.request","assets.reports"],
        "SeniorRep" or "ChurchAuth" or "Minister"=>["requisitions.view","requisitions.approve","contributions.view","reports.view","assets.view","assets.request","assets.approve","assets.reports"],
        "GroupRep"=>["requisitions.view","requisitions.initiate","contributions.view","contributions.initiate","reports.view","assets.view","assets.request"],
        "Treasurer"=>["requisitions.view","contributions.view","contributions.initiate","reports.view","assets.view","assets.request","assets.reports"],
        "Secretary"=>["requisitions.view","requisitions.initiate","contributions.view","contributions.initiate","reports.view","assets.view","assets.request","assets.approve","assets.reports"],
        _=>["requisitions.view","contributions.view","assets.view","assets.request"]
    };
}
