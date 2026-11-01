using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Platform;
using eMilimo.Core.Enums;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public interface IRolePermissionService { Task<bool> HasAsync(string? role,string permission); Task<string[]> GetAsync(string? role); Task InvalidateAsync(string role); }
public class RolePermissionService : IRolePermissionService
{
    private const string CacheVersion = "v4";
    private static readonly HashSet<string> DesignationControlledPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        "requisitions.approve", "requisitions.disburse", "contributions.confirm",
        "frb.create", "frb.edit", "frb.submit", "frb.verify", "frb.finalize",
        "service-collections.review", "service-collections.variance-approve"
    };
    private readonly AppDbContext _context; private readonly IDistributedCache _cache; private readonly RedisSettings _redis; private readonly ILogger<RolePermissionService> _logger;
    public RolePermissionService(AppDbContext context,IDistributedCache cache,RedisSettings redis,ILogger<RolePermissionService> logger)=>(_context,_cache,_redis,_logger)=(context,cache,redis,logger);
    public async Task<bool> HasAsync(string? role,string permission)=>role==UserRole.SuperAdmin.ToString()||(await GetAsync(role)).Contains(permission);
    public async Task<string[]> GetAsync(string? role)
    {
        if(string.IsNullOrWhiteSpace(role))return [];
        if(role==UserRole.SuperAdmin.ToString())return RolePermissionDefaults.All;
        var cacheKey=$"permissions:{CacheVersion}:{role.ToLowerInvariant()}";
        try
        {
            var cached=await _cache.GetStringAsync(cacheKey);
            if(cached!=null)return (JsonSerializer.Deserialize<string[]>(cached)??[])
                .Where(p=>!DesignationControlledPermissions.Contains(p)).ToArray();
        }
        catch(Exception ex){_logger.LogWarning(ex,"Permission cache read failed for role {Role}; using PostgreSQL.",role);}
        var json=await _context.RolePermissions.Where(x=>x.Role==role).Select(x=>x.PermissionsJson).FirstOrDefaultAsync();
        string[] result;
        if(json!=null)try
        {
            var stored=JsonSerializer.Deserialize<string[]>(json)??[];
            // Workflow actions are granted by the actor's active designation (or
            // an explicit built-in authority capability), never by a stale role row.
            IEnumerable<string> effective=stored.Where(p=>!DesignationControlledPermissions.Contains(p));
            if(role==UserRole.Admin.ToString())
                effective=effective.Where(p=>p is not "requisitions.initiate" and not "contributions.initiate")
                    .Concat(["reports.view","frb.view","frb.export","frb.rules.manage"]);
            // Reports are a shared read-only capability. Record-level API filters
            // still enforce each role's entity and draft visibility rules.
            if(Enum.TryParse<UserRole>(role, out _))
                effective=effective.Append("reports.view");
            if(role is nameof(UserRole.Auditor) or nameof(UserRole.ChurchAuth) or nameof(UserRole.SeniorRep))
                effective=effective.Concat(["frb.view", "frb.export"]);
            result=effective.Distinct().ToArray();
            await CacheAsync(cacheKey,result);
            return result;
        }catch(JsonException){}
        result=RolePermissionDefaults.For(role).Where(p=>!DesignationControlledPermissions.Contains(p)).ToArray();
        await CacheAsync(cacheKey,result);
        return result;
    }
    public async Task InvalidateAsync(string role){try{await Task.WhenAll(_cache.RemoveAsync($"permissions:{CacheVersion}:{role.ToLowerInvariant()}"),_cache.RemoveAsync($"permissions:{role.ToLowerInvariant()}"));}catch(Exception ex){_logger.LogWarning(ex,"Permission cache invalidation failed for role {Role}.",role);}}
    private async Task CacheAsync(string key,string[] value){try{await _cache.SetStringAsync(key,JsonSerializer.Serialize(value),new DistributedCacheEntryOptions{AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(Math.Clamp(_redis.DefaultTtlMinutes,1,1440))});}catch(Exception ex){_logger.LogWarning(ex,"Permission cache write failed for {CacheKey}.",key);}}
}
public static class RolePermissionDefaults
{
    public static readonly string[] All=["requisitions.view","requisitions.initiate","requisitions.approve","requisitions.disburse","contributions.view","contributions.initiate","contributions.confirm","groups.manage","sections.manage","committees.manage","users.manage","members.view","members.manage","members.financial-activity","membership.sync.manage","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit","service-collections.review","service-collections.variance-approve","reports.view","assets.view","assets.request","assets.manage","assets.approve","assets.reports","settings.manage","payments.developer.manage","frb.view","frb.create","frb.edit","frb.submit","frb.verify","frb.finalize","frb.export","frb.rules.manage","accounting.integration.view","accounting.integration.manage","accounting.batch.prepare","accounting.batch.approve","accounting.batch.export"];
    public static string[] For(string role)=>role switch
    {
        "SuperAdmin"=>All,
        "Admin"=>All.Where(p=>p is not "requisitions.initiate" and not "contributions.initiate" and not "payments.developer.manage").ToArray(),
        "Auditor"=>["requisitions.view","contributions.view","reports.view","assets.view","assets.reports","frb.view","frb.export","accounting.integration.view"],
        "SeniorRep"=>["requisitions.view","contributions.view","members.view","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit","reports.view","assets.view","assets.request","assets.approve","assets.reports","frb.view","frb.export"],
        "ChurchAuth"=>["requisitions.view","contributions.view","reports.view","assets.view","assets.request","assets.approve","assets.reports","frb.view","frb.export"],
        "GroupRep"=>["requisitions.view","requisitions.initiate","contributions.view","contributions.initiate","members.view","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit","reports.view","assets.view","assets.request"],
        _=>["requisitions.view","contributions.view","assets.view","assets.request"]
    };
}
