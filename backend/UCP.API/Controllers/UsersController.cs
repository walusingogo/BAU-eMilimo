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
using UCP.Core.Configuration;
using UCP.API.Services;
using UCP.API.Authorization;
using Microsoft.Extensions.Options;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILockService _lockService;
        private readonly ICurrentUserService _currentUserService;
        private readonly SecuritySettings _security;
        private readonly IEmailService _emailService;
        private readonly IMembershipIdGenerator _membershipIds;

        public UsersController(AppDbContext context, ILockService lockService, ICurrentUserService currentUserService, IOptions<SecuritySettings> security, IEmailService emailService, IMembershipIdGenerator membershipIds)
        {
            _context = context;
            _lockService = lockService;
            _currentUserService = currentUserService;
            _security = security.Value;
            _emailService = emailService;
            _membershipIds = membershipIds;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User not authenticated");
            return userId.Value;
        }

        // ============= ADMIN ENDPOINTS =============

        [HttpGet]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter)
        {
            var query = _context.Users
                .Include(u => u.Section)
                .Include(u => u.Group)
                .Include(u => u.GroupAssignments)
                    .ThenInclude(a => a.Group)
                .Include(u => u.CommitteeAssignments)
                    .ThenInclude(a => a.Committee)
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(filter.Status))
            {
                query = query.Where(u => u.Status != UserStatus.INACTIVE.ToString());
            }
            else if (!filter.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (!Enum.TryParse<UserStatus>(filter.Status, true, out var status))
                    return BadRequest(new { Message = "Invalid user status filter." });

                query = query.Where(u => u.Status == status.ToString());
            }

            if (!string.IsNullOrEmpty(filter.Role))
            {
                if (Enum.TryParse<UserRole>(filter.Role, true, out var role))
                    query = query.Where(u => u.Role == role.ToString());
            }

            if (filter.SectionId.HasValue)
                query = query.Where(u => u.SectionId == filter.SectionId.Value);

            if (filter.GroupId.HasValue)
                query = query.Where(u => u.GroupId == filter.GroupId.Value ||
                    u.GroupAssignments.Any(a => a.GroupId == filter.GroupId.Value));

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
                u.ProfilePhoto,
                u.UpdatedDate,
                u.LastProcessedTimeStamp,
                GroupIds = u.GroupAssignments.Select(a => a.GroupId).ToList(),
                AssignedGroups = u.GroupAssignments.Select(a => new
                {
                    a.Group.Id,
                    a.Group.GroupName,
                    a.Group.GroupType
                }).ToList(),
                CommitteeIds = u.CommitteeAssignments.Select(a => a.CommitteeId).ToList(),
                AssignedCommittees = u.CommitteeAssignments.Select(a => new
                {
                    a.Committee.Id,
                    a.Committee.CommitteeName
                }).ToList(),
                Section = u.Section != null ? new { u.Section.Id, u.Section.SectionNumber, u.Section.SectionName } : null,
                Group = u.Group != null ? new { u.Group.Id, u.Group.GroupName } : null
            }));
        }

        [HttpGet("{id}")]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Section)
                .Include(u => u.Group)
                .Include(u => u.CreatedBy)
                .Include(u => u.UpdatedBy)
                .Include(u => u.GroupAssignments)
                    .ThenInclude(a => a.Group)
                .Include(u => u.CommitteeAssignments)
                    .ThenInclude(a => a.Committee)
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
                user.Gender,
                user.Email,
                user.PhoneNumber,
                user.MembershipId,
                user.Role,
                user.Status,
                user.SectionId,
                user.GroupId,
                user.Locked,
                user.PasswordAttempts,
                user.PasswordLockDate,
                user.PasswordExpiryDate,
                user.PasswordExpired,
                user.ProfilePhoto,
                user.SignatureData,
                user.CreatedDate,
                user.UpdatedDate,
                user.LastProcessedTimeStamp,
                user.CreatedById,
                CreatedByName = user.CreatedBy == null ? null : $"{user.CreatedBy.FirstName} {user.CreatedBy.LastName}",
                user.UpdatedById,
                UpdatedByName = user.UpdatedBy == null ? null : $"{user.UpdatedBy.FirstName} {user.UpdatedBy.LastName}",
                user.Version,
                GroupIds = user.GroupAssignments.Select(a => a.GroupId).ToList(),
                AssignedGroups = user.GroupAssignments.Select(a => new
                {
                    a.Group.Id,
                    a.Group.GroupName,
                    a.Group.GroupType
                }).ToList(),
                CommitteeIds = user.CommitteeAssignments.Select(a => a.CommitteeId).ToList(),
                AssignedCommittees = user.CommitteeAssignments.Select(a => new
                {
                    a.Committee.Id,
                    a.Committee.CommitteeName
                }).ToList(),
                Section = user.Section != null ? new { user.Section.Id, user.Section.SectionNumber, user.Section.SectionName } : null,
                Group = user.Group != null ? new { user.Group.Id, user.Group.GroupName } : null
            });
        }

        [HttpPost]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { Message = "Email is required." });
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest(new { Message = "Phone number with country code is required." });

            var email = dto.Email.Trim().ToLowerInvariant();
            var phone = NormalizeInternationalPhone(dto.PhoneNumber);
            if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsedEmail) || parsedEmail.Address != email)
                return BadRequest(new { Message = "Enter a valid email address." });
            if (phone == null)
                return BadRequest(new { Message = "Phone number must include a country code, for example +260971234567." });

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email))
                return BadRequest(new { Message = "Email already exists" });

            var membershipId = await _membershipIds.GenerateAsync(dto.Role);

            var currentUserId = GetCurrentUserId();
            var temporaryPassword = GenerateTemporaryPassword();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                Gender = dto.Gender,
                Email = email,
                PhoneNumber = phone,
                MembershipId = membershipId,
                PasswordHash = BCryptHash.HashPassword(temporaryPassword),
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
            var assignedGroupIds = dto.GroupIds
                .Append(dto.GroupId ?? Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            var validGroupIds = await _context.Groups
                .Where(g => g.GroupType == "ChurchGroup" && assignedGroupIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync();
            if (validGroupIds.Count != assignedGroupIds.Count)
                return BadRequest(new { Message = "One or more assigned groups do not exist." });

            user.GroupId = validGroupIds.FirstOrDefault() == Guid.Empty ? null : validGroupIds.First();
            user.GroupAssignments = validGroupIds.Select(groupId => new UserGroupAssignment
            {
                UserId = user.Id,
                GroupId = groupId,
                AssignedDate = DateTime.UtcNow
            }).ToList();
            var validCommitteeIds = await _context.Committees
                .Where(c => dto.CommitteeIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
            if (validCommitteeIds.Count != dto.CommitteeIds.Distinct().Count())
                return BadRequest(new { Message = "One or more assigned committees do not exist." });
            user.CommitteeAssignments = validCommitteeIds.Select(committeeId => new UserCommitteeAssignment
            {
                UserId = user.Id,
                CommitteeId = committeeId,
                AssignedDate = DateTime.UtcNow
            }).ToList();
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.MembershipId,
                TemporaryPassword = temporaryPassword,
                user.Role,
                user.Status
            });
        }

        [HttpPut("{id}")]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users
                .Include(u => u.GroupAssignments)
                .Include(u => u.CommitteeAssignments)
                .FirstOrDefaultAsync(u => u.Id == id);
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
            if (!string.IsNullOrWhiteSpace(dto.Gender)) user.Gender = dto.Gender.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim().ToLowerInvariant();
                if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsedEmail) || parsedEmail.Address != email)
                    return BadRequest(new { Message = "Enter a valid email address." });
                if (await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email))
                    return BadRequest(new { Message = "Email already exists" });
                user.Email = email;
            }
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var phone = NormalizeInternationalPhone(dto.PhoneNumber);
                if (phone == null)
                    return BadRequest(new { Message = "Phone number must include a country code, for example +260971234567." });
                user.PhoneNumber = phone;
            }
            if (dto.SectionId.HasValue) user.SectionId = dto.SectionId;
            if (dto.GroupId.HasValue) user.GroupId = dto.GroupId;

            if (dto.GroupIds != null)
            {
                var requestedIds = dto.GroupIds.Distinct().ToList();
                var validIds = await _context.Groups
                    .Where(g => g.GroupType == "ChurchGroup" && requestedIds.Contains(g.Id))
                    .Select(g => g.Id)
                    .ToListAsync();
                if (validIds.Count != requestedIds.Count)
                    return BadRequest(new { Message = "One or more assigned groups do not exist." });

                _context.UserGroupAssignments.RemoveRange(user.GroupAssignments);
                user.GroupAssignments = validIds.Select(groupId => new UserGroupAssignment
                {
                    UserId = user.Id,
                    GroupId = groupId,
                    AssignedDate = DateTime.UtcNow
                }).ToList();
                user.GroupId = validIds.FirstOrDefault() == Guid.Empty ? null : validIds.First();
            }

            if (dto.CommitteeIds != null)
            {
                var requestedIds = dto.CommitteeIds.Distinct().ToList();
                var validIds = await _context.Committees.Where(c => requestedIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
                if (validIds.Count != requestedIds.Count)
                    return BadRequest(new { Message = "One or more assigned committees do not exist." });
                _context.UserCommitteeAssignments.RemoveRange(user.CommitteeAssignments);
                user.CommitteeAssignments = validIds.Select(committeeId => new UserCommitteeAssignment
                {
                    UserId = user.Id,
                    CommitteeId = committeeId,
                    AssignedDate = DateTime.UtcNow
                }).ToList();
            }

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
        [Authorize, RequirePermission("users.manage")]
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

            await _emailService.SendAccountActivatedAsync(user.Email, $"{user.FirstName} {user.LastName}");

            return Ok(new { Message = "User activated successfully" });
        }

        [HttpPost("{id}/deactivate")]
        [Authorize, RequirePermission("users.manage")]
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

            if (user.Role == UserRole.Admin.ToString() && user.Status == UserStatus.ACTIVE.ToString())
            {
                var adminCount = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Admin.ToString() && u.Status == UserStatus.ACTIVE.ToString());

                if (adminCount <= 1)
                    return BadRequest(new { Message = "Cannot deactivate the last Admin" });
            }

            user.Status = UserStatus.INACTIVE.ToString();
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _emailService.SendAccountDeactivatedAsync(user.Email, $"{user.FirstName} {user.LastName}");

            return Ok(new { Message = "User deactivated successfully" });
        }

        [HttpPost("{id}/unlock")]
        [Authorize, RequirePermission("users.manage")]
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
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> ResetUserPassword(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            var temporaryPassword = GenerateTemporaryPassword();
            user.PasswordHash = BCryptHash.HashPassword(temporaryPassword);
            user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
            user.PasswordExpired = false;
            user.PasswordAttempts = 0;
            user.Locked = false;
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password reset successfully.", TemporaryPassword = temporaryPassword });
        }

        [HttpPost("{id}/send-password")]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> SendTemporaryPassword(Guid id, [FromBody] SendTemporaryPasswordRequest request)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (user == null) return NotFound(new { Message = "User not found." });
            if (string.IsNullOrWhiteSpace(user.Email)) return BadRequest(new { Message = "The user does not have an email address." });
            if (string.IsNullOrWhiteSpace(request.TemporaryPassword)) return BadRequest(new { Message = "Temporary password is required." });

            await _emailService.SendTemporaryPasswordAsync(user.Email, $"{user.FirstName} {user.LastName}".Trim(), request.TemporaryPassword);
            return Accepted(new { Message = "Temporary password email queued successfully." });
        }

        public sealed record SendTemporaryPasswordRequest(string TemporaryPassword);

        private string GenerateTemporaryPassword()
        {
            var policy = _security.PasswordPolicy;
            var required = new List<char>();
            static char Pick(string characters) => characters[System.Security.Cryptography.RandomNumberGenerator.GetInt32(characters.Length)];
            if (policy.RequireUppercase) required.Add(Pick("ABCDEFGHJKLMNPQRSTUVWXYZ"));
            if (policy.RequireLowercase) required.Add(Pick("abcdefghijkmnopqrstuvwxyz"));
            if (policy.RequireDigit) required.Add(Pick("23456789"));
            if (policy.RequireSpecialCharacter) required.Add(Pick("!@#$%&*?"));
            const string all = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*?";
            while (required.Count < Math.Max(policy.MinimumLength, 12)) required.Add(Pick(all));
            for (var i = required.Count - 1; i > 0; i--) { var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1); (required[i], required[j]) = (required[j], required[i]); }
            return new string(required.ToArray());
        }

        [HttpPost("{id}/set-password")]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> AdminSetPassword(Guid id, [FromBody] AdminSetPasswordDto dto)
        {
            var policyError = PasswordPolicyValidator.Validate(dto.NewPassword, _security.PasswordPolicy);
            if (policyError != null) return BadRequest(new { Message = policyError });
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.PasswordHash = BCryptHash.HashPassword(dto.NewPassword);
            user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
            user.PasswordExpired = false;
            user.PasswordAttempts = 0;
            user.Locked = false;
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Password set successfully" });
        }

        [HttpPost("{id}/upload-photo")]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> AdminUploadProfilePhoto(Guid id, IFormFile? file)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Clear photo if file is null or empty
            if (file == null || file.Length == 0)
            {
                user.ProfilePhoto = "";
                user.UpdatedById = GetCurrentUserId();
                user.UpdatedDate = DateTime.UtcNow;
                user.LastProcessedTimeStamp = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Profile photo cleared successfully" });
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { Message = "Only JPEG, PNG, and GIF images are allowed" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { Message = "File size exceeds 5MB limit" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var dataUrl = $"data:{file.ContentType};base64,{base64}";

            user.ProfilePhoto = dataUrl;
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profile photo updated successfully" });
        }

        [HttpPost("{id}/upload-signature")]
        [Authorize, RequirePermission("users.manage")]
        public async Task<IActionResult> AdminUploadSignature(Guid id, IFormFile? file)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // Clear signature if file is null or empty
            if (file == null || file.Length == 0)
            {
                user.SignatureData = "";
                user.UpdatedById = GetCurrentUserId();
                user.UpdatedDate = DateTime.UtcNow;
                user.LastProcessedTimeStamp = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Signature cleared successfully" });
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { Message = "Only JPEG, PNG, and GIF images are allowed" });

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { Message = "File size exceeds 2MB limit" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var dataUrl = $"data:{file.ContentType};base64,{base64}";

            user.SignatureData = dataUrl;
            user.UpdatedById = GetCurrentUserId();
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Signature updated successfully" });
        }

        private static string? NormalizeInternationalPhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var normalized = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"[\s()-]", "");
            return System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\+[1-9]\d{7,14}$") ? normalized : null;
        }

        [HttpGet("{id}/signature")]
        public async Task<IActionResult> GetSignature(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
                return Forbid();

            var signature = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => u.SignatureData)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(signature))
                return NotFound(new { Message = "No signature is on file." });

            return Ok(signature);
        }

        // ============= USER SELF-SERVICE ENDPOINTS =============

        [HttpGet("signature-status")]
        public async Task<IActionResult> GetCurrentUserSignatureStatus()
        {
            var userId = GetCurrentUserId();
            var status = await _context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { HasSignature = u.SignatureData != null && u.SignatureData != "", u.SignatureData })
                .FirstOrDefaultAsync();
            return status == null ? Unauthorized() : Ok(status);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users
                .Include(u => u.Section)
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                user.Id,
                user.Title,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                user.Gender,
                user.Email,
                user.PhoneNumber,
                user.MembershipId,
                user.Role,
                user.Status,
                user.SectionId,
                user.GroupId,
                user.ProfilePhoto,
                user.SignatureData,
                Section = user.Section != null ? new { user.Section.Id, user.Section.SectionNumber, user.Section.SectionName } : null,
                Group = user.Group != null ? new { user.Group.Id, user.Group.GroupName } : null
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var policyError = PasswordPolicyValidator.Validate(dto.NewPassword, _security.PasswordPolicy);
            if (policyError != null) return BadRequest(new { Message = policyError });
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            if (!BCryptHash.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { Message = "Current password is incorrect" });

            user.PasswordHash = BCryptHash.HashPassword(dto.NewPassword);
            user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
            user.PasswordExpired = false;
            user.PasswordAttempts = 0;
            user.Locked = false;
            user.UpdatedById = userId;
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Password changed successfully" });
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile? file)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            // Clear photo if file is null or empty
            if (file == null || file.Length == 0)
            {
                user.ProfilePhoto = "";
                user.UpdatedById = userId;
                user.UpdatedDate = DateTime.UtcNow;
                user.LastProcessedTimeStamp = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Profile photo cleared successfully" });
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { Message = "Only JPEG, PNG, and GIF images are allowed" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { Message = "File size exceeds 5MB limit" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var dataUrl = $"data:{file.ContentType};base64,{base64}";

            user.ProfilePhoto = dataUrl;
            user.UpdatedById = userId;
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Profile photo updated successfully" });
        }

        [HttpPost("upload-signature")]
        public async Task<IActionResult> UploadSignature(IFormFile? file)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            // Clear signature if file is null or empty
            if (file == null || file.Length == 0)
            {
                user.SignatureData = "";
                user.UpdatedById = userId;
                user.UpdatedDate = DateTime.UtcNow;
                user.LastProcessedTimeStamp = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Signature cleared successfully" });
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { Message = "Only JPEG, PNG, and GIF images are allowed" });

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { Message = "File size exceeds 2MB limit" });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            var dataUrl = $"data:{file.ContentType};base64,{base64}";

            user.SignatureData = dataUrl;
            user.UpdatedById = userId;
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Signature updated successfully" });
        }
    }
}
