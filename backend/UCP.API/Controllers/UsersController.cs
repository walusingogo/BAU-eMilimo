using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCryptHash = BCrypt.Net.BCrypt;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.Core.Entities.Companions;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILockService _lockService;
        private readonly ICurrentUserService _currentUserService;

        public UsersController(AppDbContext context, ILockService lockService, ICurrentUserService currentUserService)
        {
            _context = context;
            _lockService = lockService;
            _currentUserService = currentUserService;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User not authenticated");
            return userId.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
        {
            var query = _context.Users
                .Include(u => u.Section)
                .Include(u => u.Group)
                .Where(u => u.Status != UserStatus.INACTIVE.ToString());

            if (!string.IsNullOrEmpty(filter.Role))
            {
                if (Enum.TryParse<UserRole>(filter.Role, true, out var role))
                    query = query.Where(u => u.Role == role.ToString());
            }

            if (filter.SectionId.HasValue)
                query = query.Where(u => u.SectionId == filter.SectionId.Value);

            if (filter.GroupId.HasValue)
                query = query.Where(u => u.GroupId == filter.GroupId.Value);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.MembershipId.ToLower().Contains(search));
            }

            var users = await query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            return Ok(users.Select(u => new
            {
                u.Id,
                u.Title,
                u.FirstName,
                u.MiddleName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.MembershipId,
                u.Role,
                u.Status,
                u.SectionId,
                u.GroupId,
                u.Locked,
                Section = u.Section != null ? new { u.Section.Id, u.Section.SectionNumber, u.Section.SectionName } : null,
                Group = u.Group != null ? new { u.Group.Id, u.Group.GroupName } : null
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Section)
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Title,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.MembershipId,
                user.Role,
                user.Status,
                user.SectionId,
                user.GroupId,
                user.Locked,
                user.PasswordExpiryDate,
                user.PasswordExpired,
                Section = user.Section != null ? new { user.Section.Id, user.Section.SectionNumber, user.Section.SectionName } : null,
                Group = user.Group != null ? new { user.Group.Id, user.Group.GroupName } : null
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { Message = "Email already exists" });

            if (await _context.Users.AnyAsync(u => u.MembershipId == dto.MembershipId))
                return BadRequest(new { Message = "Membership ID already exists" });

            var currentUserId = GetCurrentUserId();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Gender = dto.Gender,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                MembershipId = dto.MembershipId,
                // BCrypt hash
                PasswordHash = BCryptHash.HashPassword("TempPassword123!"),
                Role = Enum.Parse<UserRole>(dto.Role).ToString(),
                Status = UserStatus.PENDING.ToString(),
                SectionId = dto.SectionId,
                GroupId = dto.GroupId,
                PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                PasswordExpired = false,
                PasswordAttempts = 0,
                Locked = false,
                CreatedById = currentUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedById = currentUserId,
                UpdatedDate = DateTime.UtcNow,
                LastProcessedTimeStamp = DateTime.UtcNow,
                Version = 1
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.Status
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();

            if (await _lockService.IsLockedAsync<UserLck>(id))
            {
                var ownerId = await _lockService.GetLockOwnerAsync<UserLck>(id);
                if (ownerId != currentUserId)
                    return Conflict(new { Message = "User is being edited by another user" });
            }
            else
            {
                await _lockService.AcquireLockAsync<UserLck>(id, currentUserId, 300, "Updating user");
            }

            if (!string.IsNullOrEmpty(dto.Title)) user.Title = dto.Title;
            if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.MiddleName)) user.MiddleName = dto.MiddleName;
            if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
            if (dto.SectionId.HasValue) user.SectionId = dto.SectionId;
            if (dto.GroupId.HasValue) user.GroupId = dto.GroupId;

            if (!string.IsNullOrEmpty(dto.Role))
                user.Role = Enum.Parse<UserRole>(dto.Role).ToString();

            user.UpdatedById = currentUserId;
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _lockService.ReleaseLockAsync<UserLck>(id, currentUserId);

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.Status
            });
        }

        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Status = UserStatus.ACTIVE.ToString();
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User activated successfully" });
        }

        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            if (user.Role == UserRole.SuperAdmin.ToString() && user.Status == UserStatus.ACTIVE.ToString())
            {
                var superAdminCount = await _context.Users
                    .CountAsync(u => u.Role == UserRole.SuperAdmin.ToString() && u.Status == UserStatus.ACTIVE.ToString());

                if (superAdminCount <= 1)
                    return BadRequest(new { Message = "Cannot deactivate the last Super Admin" });
            }

            user.Status = UserStatus.INACTIVE.ToString();
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User deactivated successfully" });
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Locked = false;
            user.PasswordAttempts = 0;
            user.PasswordLockDate = null;
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "User unlocked successfully" });
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // BCrypt hash
            user.PasswordHash = BCryptHash.HashPassword("TempPassword123!");
            user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
            user.PasswordExpired = false;
            user.PasswordAttempts = 0;
            user.Locked = false;
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password reset successfully. Temporary password: TempPassword123!" });
        }
    }
}