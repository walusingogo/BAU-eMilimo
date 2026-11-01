using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using eMilimo.Core.Entities;

namespace eMilimo.API.Services;

public interface IApprovalNotificationService
{
    Task NotifyAsync(string permission, string processName, string recordNumber, string summary,
        string relativeUrl, Guid? excludeUserId = null, string[]? actions = null,
        string[]? allowedRoles = null, Guid? groupId = null, Guid? sectionId = null, Guid? committeeId = null,
        Guid? ccUserId = null, string[]? ccRoles = null, Guid[]? recipientUserIds = null,
        string[]? allowedDesignationCodes = null, string? requiredCapability = null);
}

public sealed class ApprovalNotificationService : IApprovalNotificationService
{
    private readonly AppDbContext _db;
    private readonly IRolePermissionService _permissions;
    private readonly IEmailService _email;
    private readonly ISystemLogWriter _logs;
    private readonly IWorkflowAuthorizationService _workflow;
    public ApprovalNotificationService(AppDbContext db, IRolePermissionService permissions, IEmailService email, ISystemLogWriter logs, IWorkflowAuthorizationService workflow)
        => (_db, _permissions, _email, _logs, _workflow) = (db, permissions, email, logs, workflow);

    public async Task NotifyAsync(string permission, string processName, string recordNumber, string summary,
        string relativeUrl, Guid? excludeUserId = null, string[]? actions = null,
        string[]? allowedRoles = null, Guid? groupId = null, Guid? sectionId = null, Guid? committeeId = null,
        Guid? ccUserId = null, string[]? ccRoles = null, Guid[]? recipientUserIds = null,
        string[]? allowedDesignationCodes = null, string? requiredCapability = null)
    {
        var baseUrl = await _db.SystemConfigs.AsNoTracking()
            .Where(x => x.ConfigKey == "frontend_base_url")
            .Select(x => x.ConfigValue).FirstOrDefaultAsync() ?? "http://172.20.10.5:4200";
        var url = $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        var users = await _db.Users.AsNoTracking()
            .Where(x => x.Status == "ACTIVE" && x.Email != "")
            .Select(x => new { x.Id, x.Role, x.Email, x.PhoneNumber, x.FirstName, x.LastName }).ToListAsync();
        var permittedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in users.Select(x => x.Role).Distinct())
            if (await _permissions.HasAsync(role, permission)) permittedRoles.Add(role);
        var designationUserIds = new HashSet<Guid>();
        if (allowedDesignationCodes?.Length > 0)
        {
            var now = DateTime.UtcNow;
            designationUserIds = (await _db.OfficeAppointments.AsNoTracking()
                .Where(x => allowedDesignationCodes.Contains(x.Designation.Code) && x.Designation.IsActive &&
                    x.FunctionalArea.IsActive && x.IsActive && x.EffectiveFromUtc <= now &&
                    (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc > now))
                .Select(x => x.UserId).ToListAsync()).ToHashSet();
        }
        if (!string.IsNullOrWhiteSpace(requiredCapability))
            designationUserIds.UnionWith(await _workflow.GetUserIdsAsync(requiredCapability));
        var scopedAuthorityIds = new HashSet<Guid>();
        if (!string.IsNullOrWhiteSpace(requiredCapability))
        {
            var resource = new AuthorizationResource(sectionId, groupId, committeeId);
            foreach (var candidate in designationUserIds)
                if (await _workflow.CanAsync(candidate, requiredCapability, resource)) scopedAuthorityIds.Add(candidate);
        }
        var recipients = users.Where(x => (permittedRoles.Contains(x.Role) || designationUserIds.Contains(x.Id)) && (!excludeUserId.HasValue || x.Id != excludeUserId));
        if (allowedRoles != null || designationUserIds.Count > 0)
            recipients = recipients.Where(x => (allowedRoles?.Contains(x.Role, StringComparer.OrdinalIgnoreCase) ?? false) || designationUserIds.Contains(x.Id));
        // A capability-based workflow must never be widened by a role or
        // module-permission match. Keep only users who can exercise the capability for
        // this exact resource; this mirrors the authorization check on the action.
        if (!string.IsNullOrWhiteSpace(requiredCapability))
            recipients = recipients.Where(x => scopedAuthorityIds.Contains(x.Id));
        if (recipientUserIds != null) recipients = recipients.Where(x => recipientUserIds.Contains(x.Id));
        if (groupId.HasValue)
        {
            var ids = await _db.UserGroupAssignments.Where(x => x.GroupId == groupId).Select(x => x.UserId).ToListAsync();
            var leaderId = await _db.Groups.Where(x => x.Id == groupId).Select(x => x.LeaderId).FirstOrDefaultAsync();
            if (leaderId.HasValue) ids.Add(leaderId.Value); recipients = recipients.Where(x => ids.Contains(x.Id) || scopedAuthorityIds.Contains(x.Id));
        }
        if (sectionId.HasValue) recipients = recipients.Where(x => _db.Users.Any(u => u.Id == x.Id && u.SectionId == sectionId) || scopedAuthorityIds.Contains(x.Id));
        if (committeeId.HasValue) recipients = recipients.Where(x => _db.UserCommitteeAssignments.Any(a => a.UserId == x.Id && a.CommitteeId == committeeId) || scopedAuthorityIds.Contains(x.Id));
        var ccEmails = users.Where(x => (ccUserId.HasValue && x.Id == ccUserId) || (ccRoles?.Contains(x.Role, StringComparer.OrdinalIgnoreCase) ?? false)).Select(x => x.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var resolvedRecipients = recipients.DistinctBy(x => x.Id).ToArray();
        if (resolvedRecipients.Length == 0)
        {
            await _logs.WriteAsync(new SystemLog { Level = "Warning", Category = "Email", Process = processName, Action = "ApprovalRouting", Message = $"No eligible approval email recipient was found for {processName} {recordNumber}. Workflow roles: {string.Join(", ", allowedRoles ?? [])}. Capability: {requiredCapability ?? "none"}." });
            return;
        }
        foreach (var user in resolvedRecipients)
        {
            await _email.SendApprovalActionRequiredAsync(user.Email, $"{user.FirstName} {user.LastName}".Trim(),
                processName, recordNumber, summary, url, actions ?? ["Approve", "Reject with comment"], ccEmails);
        }
    }

}
