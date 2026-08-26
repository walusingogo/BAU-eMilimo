using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using UCP.API.Platform;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

public interface IRolePermissionService { Task<bool> HasAsync(string? role,string permission); Task<string[]> GetAsync(string? role); Task InvalidateAsync(string role); }
public class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _context; private readonly IDistributedCache _cache; private readonly RedisSettings _redis; private readonly ILogger<RolePermissionService> _logger;
    public RolePermissionService(AppDbContext context,IDistributedCache cache,RedisSettings redis,ILogger<RolePermissionService> logger)=>(_context,_cache,_redis,_logger)=(context,cache,redis,logger);
    public async Task<bool> HasAsync(string? role,string permission)=>role==UserRole.SuperAdmin.ToString()||(await GetAsync(role)).Contains(permission);
    public async Task<string[]> GetAsync(string? role)
    {
        if(string.IsNullOrWhiteSpace(role))return [];
        if(role==UserRole.SuperAdmin.ToString())return RolePermissionDefaults.All;
        var cacheKey=$"permissions:{role.ToLowerInvariant()}";
        try
        {
            var cached=await _cache.GetStringAsync(cacheKey);
            if(cached!=null)return JsonSerializer.Deserialize<string[]>(cached)??[];
        }
        catch(Exception ex){_logger.LogWarning(ex,"Permission cache read failed for role {Role}; using PostgreSQL.",role);}
        var json=await _context.RolePermissions.Where(x=>x.Role==role).Select(x=>x.PermissionsJson).FirstOrDefaultAsync();
        string[] result;
        if(json!=null)try
        {
            var stored=JsonSerializer.Deserialize<string[]>(json)??[];
            if(role==UserRole.Admin.ToString())return stored
                .Where(p=>p is not "requisitions.initiate" and not "contributions.initiate")
                .Append("reports.view").Distinct().ToArray();
            // These permissions are part of the configured approval workflow,
            // so an older saved role row must not silently prevent routing or action.
            IEnumerable<string> effective=stored;
            if(role is nameof(UserRole.Accountant) or nameof(UserRole.Treasurer))
                effective=effective.Append("contributions.confirm");
            if(role is nameof(UserRole.GroupRep) or nameof(UserRole.SeniorRep) or nameof(UserRole.ChurchAuth) or
                nameof(UserRole.Treasurer) or nameof(UserRole.Accountant) or nameof(UserRole.Secretary) or nameof(UserRole.Minister))
                effective=effective.Append("requisitions.approve");
            // Reports are a shared read-only capability. Record-level API filters
            // still enforce each role's entity and draft visibility rules.
            if(Enum.TryParse<UserRole>(role, out _))
                effective=effective.Append("reports.view");
            result=effective.Distinct().ToArray();
            await CacheAsync(cacheKey,result);
            return result;
        }catch(JsonException){}
        result=RolePermissionDefaults.For(role);
        await CacheAsync(cacheKey,result);
        return result;
    }
    public async Task InvalidateAsync(string role){try{await _cache.RemoveAsync($"permissions:{role.ToLowerInvariant()}");}catch(Exception ex){_logger.LogWarning(ex,"Permission cache invalidation failed for role {Role}.",role);}}
    private async Task CacheAsync(string key,string[] value){try{await _cache.SetStringAsync(key,JsonSerializer.Serialize(value),new DistributedCacheEntryOptions{AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(Math.Clamp(_redis.DefaultTtlMinutes,1,1440))});}catch(Exception ex){_logger.LogWarning(ex,"Permission cache write failed for {CacheKey}.",key);}}
}
public static class RolePermissionDefaults
{
    public static readonly string[] All=["requisitions.view","requisitions.initiate","requisitions.approve","requisitions.disburse","contributions.view","contributions.initiate","contributions.confirm","groups.manage","sections.manage","committees.manage","users.manage","reports.view","assets.view","assets.request","assets.manage","assets.approve","assets.reports","settings.manage"];
    public static string[] For(string role)=>role switch
    {
        "SuperAdmin"=>All,
        "Admin"=>All.Where(p=>p is not "requisitions.initiate" and not "contributions.initiate").ToArray(),
        "Auditor"=>["requisitions.view","contributions.view","reports.view","assets.view","assets.reports"],
        "Accountant"=>["requisitions.view","requisitions.approve","requisitions.disburse","contributions.view","contributions.confirm","reports.view","assets.view","assets.request","assets.reports"],
        "SeniorRep"=>["requisitions.view","requisitions.approve","contributions.view","reports.view","assets.view","assets.request","assets.approve","assets.reports"],
        "ChurchAuth" or "Minister"=>["requisitions.view","requisitions.approve","contributions.view","reports.view","assets.view","assets.request","assets.approve","assets.reports"],
        "GroupRep"=>["requisitions.view","requisitions.initiate","requisitions.approve","contributions.view","contributions.initiate","reports.view","assets.view","assets.request"],
        "Treasurer"=>["requisitions.view","requisitions.approve","contributions.view","contributions.initiate","contributions.confirm","reports.view","assets.view","assets.request","assets.reports"],
        "Secretary"=>["requisitions.view","requisitions.initiate","requisitions.approve","contributions.view","contributions.initiate","reports.view","assets.view","assets.request","assets.approve","assets.reports"],
        _=>["requisitions.view","contributions.view","assets.view","assets.request"]
    };
}
