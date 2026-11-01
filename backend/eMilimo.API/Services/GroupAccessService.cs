using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Enums;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services
{
    public class GroupAccessService : IGroupAccessService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GroupAccessService(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public bool HasGlobalAccess()
        {
            var role = _currentUser.GetUserRole();
            return role == UserRole.Admin.ToString() || role == UserRole.SuperAdmin.ToString();
        }

        public async Task<List<Guid>> GetAccessibleGroupIdsAsync()
        {
            if (HasGlobalAccess())
                return await _context.Groups.Where(g => g.GroupType == "ChurchGroup").Select(g => g.Id).ToListAsync();

            var userId = _currentUser.GetUserId();
            if (!userId.HasValue)
                return new List<Guid>();

            var ids = await _context.UserGroupAssignments
                .Where(a => a.UserId == userId.Value)
                .Select(a => a.GroupId)
                .ToListAsync();

            return ids;
        }

        public async Task<bool> CanAccessGroupAsync(Guid groupId) =>
            HasGlobalAccess() || (await GetAccessibleGroupIdsAsync()).Contains(groupId);

        public async Task<List<Guid>> GetAccessibleSectionIdsAsync()
        {
            if (HasGlobalAccess()) return await _context.Sections.Select(x => x.Id).ToListAsync();
            var userId = _currentUser.GetUserId();
            if (!userId.HasValue) return new List<Guid>();
            var sectionId = await _context.Users.Where(x => x.Id == userId.Value).Select(x => x.SectionId).FirstOrDefaultAsync();
            return sectionId.HasValue ? new List<Guid> { sectionId.Value } : new List<Guid>();
        }

        public async Task<List<Guid>> GetAccessibleCommitteeIdsAsync()
        {
            if (HasGlobalAccess()) return await _context.Committees.Select(x => x.Id).ToListAsync();
            var userId = _currentUser.GetUserId();
            if (!userId.HasValue) return new List<Guid>();
            return await _context.UserCommitteeAssignments.Where(x => x.UserId == userId.Value).Select(x => x.CommitteeId).ToListAsync();
        }

        public async Task<bool> CanAccessEntityAsync(Guid? groupId, Guid? sectionId, Guid? committeeId)
        {
            if (HasGlobalAccess()) return true;
            if (groupId.HasValue) return (await GetAccessibleGroupIdsAsync()).Contains(groupId.Value);
            if (sectionId.HasValue) return (await GetAccessibleSectionIdsAsync()).Contains(sectionId.Value);
            if (committeeId.HasValue) return (await GetAccessibleCommitteeIdsAsync()).Contains(committeeId.Value);
            return false;
        }
    }
}
