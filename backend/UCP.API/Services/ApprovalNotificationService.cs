using Microsoft.EntityFrameworkCore;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.Core.Entities;

namespace UCP.API.Services;

public interface IApprovalNotificationService
{
    Task NotifyAsync(string permission, string processName, string recordNumber, string summary,
        string relativeUrl, Guid? excludeUserId = null, string[]? actions = null,
        string[]? allowedRoles = null, Guid? groupId = null, Guid? sectionId = null, Guid? committeeId = null,
        Guid? ccUserId = null, string[]? ccRoles = null, Guid[]? recipientUserIds = null);
}

public sealed class ApprovalNotificationService : IApprovalNotificationService
{
    private readonly AppDbContext _db;
    private readonly IRolePermissionService _permissions;
    private readonly IEmailService _email;
    private readonly ISystemLogWriter _logs;
    public ApprovalNotificationService(AppDbContext db, IRolePermissionService permissions, IEmailService email, ISystemLogWriter logs)
        => (_db, _permissions, _email, _logs) = (db, permissions, email, logs);

    public async Task NotifyAsync(string permission, string processName, string recordNumber, string summary,
        string relativeUrl, Guid? excludeUserId = null, string[]? actions = null,
        string[]? allowedRoles = null, Guid? groupId = null, Guid? sectionId = null, Guid? committeeId = null,
        Guid? ccUserId = null, string[]? ccRoles = null, Guid[]? recipientUserIds = null)
    {
        var baseUrl = await _db.SystemConfigs.AsNoTracking()
            .Where(x => x.ConfigKey == "frontend_base_url")
            .Select(x => x.ConfigValue).FirstOrDefaultAsync() ?? "http://172.20.10.5:4200";
        var url = $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        var users = await _db.Users.AsNoTracking()
            .Where(x => x.Status == "ACTIVE" && x.Email != "")
            .Select(x => new { x.Id, x.Role, x.Email, x.PhoneNumber, x.FirstName, x.LastName }).ToListAsync();
        var policyPrefix = processName.Equals("Contribution", StringComparison.OrdinalIgnoreCase) ? "notification_contribution" :
            processName.Equals("Requisition", StringComparison.OrdinalIgnoreCase) ? "notification_requisition" : "notification_asset_request";
        allowedRoles = await ConfiguredRoles($"{policyPrefix}_to_roles", allowedRoles);
        var configuredCcRoles = await ConfiguredRoles($"{policyPrefix}_cc_roles", []);
        ccRoles = (configuredCcRoles ?? []).Concat(ccRoles ?? []).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var permittedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in users.Select(x => x.Role).Distinct())
            if (await _permissions.HasAsync(role, permission)) permittedRoles.Add(role);
        var recipients = users.Where(x => permittedRoles.Contains(x.Role) && (!excludeUserId.HasValue || x.Id != excludeUserId));
        if (allowedRoles != null) recipients = recipients.Where(x => allowedRoles.Contains(x.Role, StringComparer.OrdinalIgnoreCase));
        if (recipientUserIds != null) recipients = recipients.Where(x => recipientUserIds.Contains(x.Id));
        if (groupId.HasValue)
        {
            var ids = await _db.UserGroupAssignments.Where(x => x.GroupId == groupId).Select(x => x.UserId).ToListAsync();
            var legacy = await _db.Users.Where(x => x.GroupId == groupId).Select(x => x.Id).ToListAsync();
            var leaderId = await _db.Groups.Where(x => x.Id == groupId).Select(x => x.LeaderId).FirstOrDefaultAsync();
            ids.AddRange(legacy); if (leaderId.HasValue) ids.Add(leaderId.Value); recipients = recipients.Where(x => ids.Contains(x.Id));
        }
        if (sectionId.HasValue) recipients = recipients.Where(x => _db.Users.Any(u => u.Id == x.Id && u.SectionId == sectionId));
        if (committeeId.HasValue) recipients = recipients.Where(x => _db.UserCommitteeAssignments.Any(a => a.UserId == x.Id && a.CommitteeId == committeeId));
        var ccEmails = users.Where(x => (ccUserId.HasValue && x.Id == ccUserId) || (ccRoles?.Contains(x.Role, StringComparer.OrdinalIgnoreCase) ?? false)).Select(x => x.Email).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var resolvedRecipients = recipients.DistinctBy(x => x.Id).ToArray();
        if (resolvedRecipients.Length == 0)
        {
            await _logs.WriteAsync(new SystemLog { Level = "Warning", Category = "Email", Process = processName, Action = "ApprovalRouting", Message = $"No eligible approval email recipient was found for {processName} {recordNumber}. Configured To roles: {string.Join(", ", allowedRoles ?? [])}." });
            return;
        }
        foreach (var user in resolvedRecipients)
        {
            await _email.SendApprovalActionRequiredAsync(user.Email, $"{user.FirstName} {user.LastName}".Trim(),
                processName, recordNumber, summary, url, actions ?? ["Approve", "Reject with comment"], ccEmails);
        }
    }

    private async Task<string[]?> ConfiguredRoles(string key, string[]? fallback)
    {
        var configured = await _db.SystemConfigs.AsNoTracking().Where(x => !x.IsArchived && x.ConfigKey == key).Select(x => x.ConfigValue).FirstOrDefaultAsync();
        if (configured == null) return fallback;
        return configured.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
