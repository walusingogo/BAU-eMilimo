using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

public interface IWorkflowAuthorizationService
{
    Task<bool> HasAsync(Guid userId, string capability, CancellationToken ct = default);
    Task<bool> CanAsync(Guid userId, string capability, AuthorizationResource? resource, CancellationToken ct = default);
    Task<string[]> GetCapabilitiesAsync(Guid userId, CancellationToken ct = default);
    Task<string[]> GetPermissionKeysAsync(Guid userId, CancellationToken ct = default);
    Task<Guid[]> GetUserIdsAsync(string capability, CancellationToken ct = default);
    Task<bool> HasCongregationScopeAsync(Guid userId, CancellationToken ct = default);
}

public sealed record AuthorizationResource(
    Guid? SectionId = null,
    Guid? GroupId = null,
    Guid? CommitteeId = null,
    Guid? FunctionalAreaId = null,
    string? FunctionalAreaCode = null);

public sealed class WorkflowAuthorizationService(AppDbContext db) : IWorkflowAuthorizationService
{
    public async Task<bool> HasCongregationScopeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.Role, x.Status }).FirstOrDefaultAsync(ct);
        if (user == null || user.Status != UserStatus.ACTIVE.ToString()) return false;
        if (user.Role is nameof(UserRole.SuperAdmin) or nameof(UserRole.Admin) or nameof(UserRole.Auditor)) return true;
        var now = DateTime.UtcNow;
        return await db.OfficeAppointments.AsNoTracking().AnyAsync(x => x.UserId == userId && x.IsActive &&
            x.ScopeType == AuthorityScopeTypes.Congregation && x.EffectiveFromUtc <= now &&
            (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) && x.Designation.IsActive &&
            x.FunctionalArea.IsActive && x.Designation.RequiredRole == user.Role, ct);
    }
    public async Task<bool> HasAsync(Guid userId, string capability, CancellationToken ct = default) =>
        (await GetCapabilitiesAsync(userId, ct)).Contains(capability, StringComparer.OrdinalIgnoreCase);

    public async Task<bool> CanAsync(Guid userId, string capability, AuthorizationResource? resource, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new { x.Role, x.Status }).FirstOrDefaultAsync(ct);
        if (user == null || user.Status != UserStatus.ACTIVE.ToString()) return false;
        if (user.Role == nameof(UserRole.SuperAdmin)) return true;

        var now = DateTime.UtcNow;
        var appointments = await db.OfficeAppointments.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EffectiveFromUtc <= now &&
                (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) && x.Designation.IsActive &&
                x.FunctionalArea.IsActive)
            .Select(x => new
            {
                x.ScopeType, x.SectionId, x.GroupId, x.CommitteeId, x.FunctionalAreaId,
                FunctionalAreaCode = x.FunctionalArea.Code, x.Designation.RequiredRole,
                DesignationCode = x.Designation.Code,
                x.Designation.CapabilitiesJson
            }).ToListAsync(ct);
        if (appointments.Any(x => user.Role.Equals(x.RequiredRole, StringComparison.OrdinalIgnoreCase) &&
            Read(x.CapabilitiesJson).Contains(capability, StringComparer.OrdinalIgnoreCase) &&
            (IsCongregationWideStewardshipCheck(capability, x.DesignationCode) || ScopeMatches(x.ScopeType,
                x.SectionId, x.GroupId, x.CommitteeId, x.FunctionalAreaId, x.FunctionalAreaCode, resource)))) return true;
        return false;
    }

    public async Task<string[]> GetCapabilitiesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new { x.Role, x.Status }).FirstOrDefaultAsync(ct);
        if (user == null || user.Status != UserStatus.ACTIVE.ToString()) return [];
        if (user.Role == nameof(UserRole.SuperAdmin)) return WorkflowCapabilities.All;

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var appointmentCapabilities = await db.OfficeAppointments.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EffectiveFromUtc <= now &&
                (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) && x.Designation.IsActive && x.FunctionalArea.IsActive)
            .Select(x => new { x.Designation.RequiredRole, x.Designation.CapabilitiesJson }).ToListAsync(ct);
        foreach (var appointment in appointmentCapabilities.Where(x => user.Role.Equals(x.RequiredRole, StringComparison.OrdinalIgnoreCase)))
            foreach (var capability in Read(appointment.CapabilitiesJson)) result.Add(capability);
        return result.ToArray();
    }

    public async Task<string[]> GetPermissionKeysAsync(Guid userId, CancellationToken ct = default)
    {
        var capabilities = await GetCapabilitiesAsync(userId, ct);
        return capabilities.SelectMany(PermissionsFor).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<Guid[]> GetUserIdsAsync(string capability, CancellationToken ct = default)
    {
        var activeUsers = await db.Users.AsNoTracking().Where(x => x.Status == UserStatus.ACTIVE.ToString())
            .Select(x => new { x.Id, x.Role }).ToListAsync(ct);
        var roleUsers = activeUsers.Where(x => x.Role == nameof(UserRole.SuperAdmin)).Select(x => x.Id);
        var now = DateTime.UtcNow;
        var appointments = await db.OfficeAppointments.AsNoTracking()
            .Where(x => x.User.Status == UserStatus.ACTIVE.ToString() && x.IsActive && x.EffectiveFromUtc <= now &&
                (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now) && x.Designation.IsActive && x.FunctionalArea.IsActive)
            .Select(x => new { x.UserId, x.User.Role, x.Designation.RequiredRole, x.Designation.CapabilitiesJson }).ToListAsync(ct);
        var appointmentUsers = appointments.Where(x => x.Role.Equals(x.RequiredRole, StringComparison.OrdinalIgnoreCase) &&
            Read(x.CapabilitiesJson).Contains(capability, StringComparer.OrdinalIgnoreCase)).Select(x => x.UserId);
        return roleUsers.Concat(appointmentUsers).Distinct().ToArray();
    }

    public static string[] PermissionsFor(string capability) => capability switch
    {
        WorkflowCapabilities.RequisitionEntityReview or WorkflowCapabilities.RequisitionChurchAuthority or
        WorkflowCapabilities.RequisitionTreasurerApproval or WorkflowCapabilities.RequisitionMinisterApproval => ["requisitions.view", "requisitions.approve"],
        WorkflowCapabilities.RequisitionAccountantApproval => ["requisitions.view", "requisitions.approve", "requisitions.disburse"],
        WorkflowCapabilities.ContributionReview => ["contributions.view", "contributions.confirm"],
        WorkflowCapabilities.FrbManage => ["frb.view", "frb.create", "frb.edit", "frb.submit", "frb.export"],
        WorkflowCapabilities.FrbVerifyStewardship or WorkflowCapabilities.FrbVerifyTreasurer or WorkflowCapabilities.FrbVerifyMinister => ["frb.view", "frb.verify", "frb.export"],
        WorkflowCapabilities.FrbFinalize => ["frb.view", "frb.finalize", "frb.export"],
        WorkflowCapabilities.FrbOrganizationScope => ["frb.view", "frb.export"],
        WorkflowCapabilities.ServiceCollectionManage => ["service-collections.view", "service-collections.create", "service-collections.edit", "service-collections.submit"],
        WorkflowCapabilities.ServiceCollectionReview => ["service-collections.view", "service-collections.review"],
        WorkflowCapabilities.ServiceCollectionVarianceApprove => ["service-collections.view", "service-collections.variance-approve"],
        _ => []
    };

    private static string[] Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static bool ScopeMatches(string scopeType, Guid? sectionId, Guid? groupId, Guid? committeeId,
        Guid functionalAreaId, string functionalAreaCode, AuthorizationResource? resource)
    {
        if (resource == null) return true;
        var areaMatches = !resource.FunctionalAreaId.HasValue && string.IsNullOrWhiteSpace(resource.FunctionalAreaCode) ||
            resource.FunctionalAreaId == functionalAreaId ||
            (!string.IsNullOrWhiteSpace(resource.FunctionalAreaCode) && resource.FunctionalAreaCode.Equals(functionalAreaCode, StringComparison.OrdinalIgnoreCase));
        return scopeType switch
        {
            AuthorityScopeTypes.Congregation => true,
            AuthorityScopeTypes.FunctionalArea => areaMatches,
            AuthorityScopeTypes.Section => sectionId.HasValue && resource.SectionId == sectionId,
            AuthorityScopeTypes.Group => groupId.HasValue && resource.GroupId == groupId,
            AuthorityScopeTypes.Committee => committeeId.HasValue && resource.CommitteeId == committeeId,
            AuthorityScopeTypes.Entity => sectionId.HasValue && resource.SectionId == sectionId ||
                                          groupId.HasValue && resource.GroupId == groupId ||
                                          committeeId.HasValue && resource.CommitteeId == committeeId,
            _ => false
        };
    }

    private static bool IsCongregationWideStewardshipCheck(string capability, string designationCode) =>
        designationCode.Equals(DesignationCodes.Steward, StringComparison.OrdinalIgnoreCase) &&
        capability is WorkflowCapabilities.ServiceCollectionReview or WorkflowCapabilities.ServiceCollectionVarianceApprove;

}
