using Microsoft.EntityFrameworkCore;
using UCP.Core.Services;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

public interface IApprovalNotificationService
{
    Task NotifyAsync(string permission, string processName, string recordNumber, string summary,
        string relativeUrl, Guid? excludeUserId = null, string[]? actions = null,
        string[]? allowedRoles = null, Guid? groupId = null, Guid? sectionId = null, Guid? committeeId = null);
}

public sealed class ApprovalNotificationService : IApprovalNotificationService
{
    private readonly AppDbContext _db;
    private readonly IRolePermissionService _permissions;
    private readonly IEmailService _email;

    public ApprovalNotificationService(AppDbContext db, IRolePermissionService permissions, IEmailService email)
        => (_db, _permissions, _email) = (db, permissions, email);

    public async Task NotifyAsync(string permission, string processName, string recordNumber, string summary,
        string relativeUrl, Guid? excludeUserId = null, string[]? actions = null,
        string[]? allowedRoles = null, Guid? groupId = null, Guid? sectionId = null, Guid? committeeId = null)
    {
        var baseUrl = await _db.SystemConfigs.AsNoTracking()
            .Where(x => x.ConfigKey == "frontend_base_url")
            .Select(x => x.ConfigValue).FirstOrDefaultAsync() ?? "http://192.168.1.178:4200";
        var url = $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
        var users = await _db.Users.AsNoTracking()
            .Where(x => x.Status == "ACTIVE" && x.Email != "" && (!excludeUserId.HasValue || x.Id != excludeUserId))
            .Select(x => new { x.Id, x.Role, x.Email, x.FirstName, x.LastName }).ToListAsync();
        var permittedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in users.Select(x => x.Role).Distinct())
            if (await _permissions.HasAsync(role, permission)) permittedRoles.Add(role);
        var recipients = users.Where(x => permittedRoles.Contains(x.Role));
        if (allowedRoles?.Length > 0) recipients = recipients.Where(x => allowedRoles.Contains(x.Role));
        if (groupId.HasValue)
        {
            var ids = await _db.UserGroupAssignments.Where(x => x.GroupId == groupId).Select(x => x.UserId).ToListAsync();
            var legacy = await _db.Users.Where(x => x.GroupId == groupId).Select(x => x.Id).ToListAsync();
            ids.AddRange(legacy); recipients = recipients.Where(x => ids.Contains(x.Id));
        }
        if (sectionId.HasValue) recipients = recipients.Where(x => _db.Users.Any(u => u.Id == x.Id && u.SectionId == sectionId));
        if (committeeId.HasValue) recipients = recipients.Where(x => _db.UserCommitteeAssignments.Any(a => a.UserId == x.Id && a.CommitteeId == committeeId));
        foreach (var user in recipients)
            await _email.SendApprovalActionRequiredAsync(user.Email, $"{user.FirstName} {user.LastName}".Trim(),
                processName, recordNumber, summary, url, actions ?? ["Approve", "Reject with comment"]);
    }
}
