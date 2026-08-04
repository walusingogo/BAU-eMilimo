using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

public interface IRolePermissionService { Task<bool> HasAsync(string? role,string permission); Task<string[]> GetAsync(string? role); }
public class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _context; private const string Prefix="RolePermissions:";
    public RolePermissionService(AppDbContext context)=>_context=context;
    public async Task<bool> HasAsync(string? role,string permission)=>role==UserRole.SuperAdmin.ToString()||(await GetAsync(role)).Contains(permission);
    public async Task<string[]> GetAsync(string? role)
    {
        if(string.IsNullOrWhiteSpace(role))return [];
        if(role==UserRole.SuperAdmin.ToString())return RolePermissionDefaults.All;
        var json=await _context.SystemConfigs.Where(x=>x.ConfigKey==Prefix+role&&!x.IsArchived).Select(x=>x.ConfigValue).FirstOrDefaultAsync();
        if(json!=null)try{return JsonSerializer.Deserialize<string[]>(json)??[];}catch(JsonException){}
        return RolePermissionDefaults.For(role);
    }
}
public static class RolePermissionDefaults
{
    public static readonly string[] All=["requisitions.view","requisitions.initiate","requisitions.approve","requisitions.disburse","contributions.view","contributions.initiate","contributions.confirm","groups.manage","sections.manage","committees.manage","users.manage","reports.view"];
    public static string[] For(string role)=>role switch
    {
        "SuperAdmin"=>All,
        "Admin"=>All,
        "Accountant"=>["requisitions.view","requisitions.disburse","contributions.view","contributions.confirm","reports.view"],
        "SeniorRep" or "ChurchAuth" or "Minister"=>["requisitions.view","requisitions.approve","contributions.view","reports.view"],
        "GroupRep"=>["requisitions.view","requisitions.initiate","contributions.view","contributions.initiate","reports.view"],
        "Treasurer"=>["requisitions.view","contributions.view","contributions.initiate","reports.view"],
        "Secretary"=>["requisitions.view","requisitions.initiate","contributions.view","contributions.initiate","reports.view"],
        _=>["requisitions.view","contributions.view"]
    };
}
