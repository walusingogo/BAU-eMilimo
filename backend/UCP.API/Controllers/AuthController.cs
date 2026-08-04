using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCryptHash = BCrypt.Net.BCrypt;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.Core.Configuration;
using UCP.API.Services;
using Microsoft.Extensions.Options;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ICurrentUserService _currentUserService;
        private readonly SecuritySettings _security;

        public AuthController(AppDbContext context, IConfiguration config, ICurrentUserService currentUserService, IOptions<SecuritySettings> security)
        {
            _context = context;
            _config = config;
            _currentUserService = currentUserService;
            _security = security.Value;
        }

        private string GetJwtSecret()
        {
            return _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var identifier = (dto.Identifier ?? dto.Email)?.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
                return BadRequest(new { Message = "Email address or Membership ID is required." });

            var normalizedIdentifier = identifier.ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedIdentifier || u.MembershipId.ToLower() == normalizedIdentifier);

            if (user == null)
                return Unauthorized(new { Message = "Invalid email, Membership ID, or password" });

            // Account locked
            if (user.Locked)
                return Unauthorized(new { Message = "Account is locked. Please contact administrator." });

            // Check if user is ACTIVE
            if (!Enum.TryParse<UserStatus>(user.Status, out var statusEnum) || statusEnum != UserStatus.ACTIVE)
            {
                string statusMessage = statusEnum switch
                {
                    UserStatus.INACTIVE => "Your account has been deactivated. Please contact administrator.",
                    UserStatus.PENDING => "Your account is pending activation. Please wait for approval.",
                    _ => "Your account is not active. Please contact administrator."
                };
                return Unauthorized(new { Message = statusMessage });
            }

            // Verify password
            if (!BCryptHash.Verify(dto.Password, user.PasswordHash))
            {
                user.PasswordAttempts++;
                if (user.PasswordAttempts >= 5)
                {
                    user.Locked = true;
                    user.PasswordLockDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Unauthorized(new { Message = "Account locked due to multiple failed attempts." });
                }
                await _context.SaveChangesAsync();
                return Unauthorized(new { Message = "Invalid email, Membership ID, or password" });
            }

            // Reset attempts, update timestamps, and generate a new session ID
            user.PasswordAttempts = 0;
            user.LastProcessedTimeStamp = DateTime.UtcNow;
            user.CurrentSessionId = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.Status,
                    user.Gender,                      // <-- Added
                    user.SectionId,
                    user.GroupId,
                    user.ProfilePhoto
                }
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = _currentUserService.GetUserId();
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    user.CurrentSessionId = null;
                    await _context.SaveChangesAsync();
                }
            }
            return Ok(new { Message = "Logged out successfully" });
        }

        [HttpGet("client-settings")]
        public IActionResult GetClientSettings() => Ok(new
        {
            session = _security.Session,
            passwordPolicy = _security.PasswordPolicy
        });

        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> RefreshSession()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue) return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null || user.Locked || user.Status != UserStatus.ACTIVE.ToString())
                return Unauthorized(new { Message = "Your account is not active or has been locked." });

            var sessionId = user.CurrentSessionId ?? Guid.NewGuid().ToString();
            user.LastProcessedTimeStamp = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.Status,
                    user.Gender,                      // <-- Added
                    user.SectionId,
                    user.GroupId,
                    user.ProfilePhoto
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Section)
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Role,
                user.Status,
                user.Gender,                      // <-- Added
                user.SectionId,
                user.GroupId,
                Section = user.Section != null ? new { user.Section.Id, user.Section.SectionNumber, user.Section.SectionName } : null,
                Group = user.Group != null ? new { user.Group.Id, user.Group.GroupName } : null
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var policyError = PasswordPolicyValidator.Validate(dto.NewPassword, _security.PasswordPolicy);
            if (policyError != null) return BadRequest(new { Message = policyError });

            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            if (user.Status != UserStatus.ACTIVE.ToString() || user.Locked)
                return Unauthorized(new { Message = "Your account is not active or locked." });

            if (!BCryptHash.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { Message = "Current password is incorrect" });

            user.PasswordHash = BCryptHash.HashPassword(dto.NewPassword);
            user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
            user.PasswordExpired = false;
            user.PasswordAttempts = 0;
            user.UpdatedDate = DateTime.UtcNow;
            user.LastProcessedTimeStamp = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Password changed successfully" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return Ok(new { Message = "If an account exists, a reset link will be sent" });

            var secret = GetJwtSecret();
            var key = Encoding.ASCII.GetBytes(secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("type", "password_reset")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var resetToken = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Message = "Password reset link sent to your email",
                Token = resetToken
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                var policyError = PasswordPolicyValidator.Validate(dto.NewPassword, _security.PasswordPolicy);
                if (policyError != null) return BadRequest(new { Message = policyError });

                var secret = GetJwtSecret();
                var key = Encoding.ASCII.GetBytes(secret);
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(dto.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = Guid.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
                var tokenType = jwtToken.Claims.First(x => x.Type == "type").Value;

                if (tokenType != "password_reset")
                    return BadRequest(new { Message = "Invalid token" });

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound();

                user.PasswordHash = BCryptHash.HashPassword(dto.NewPassword);
                user.PasswordExpiryDate = DateTime.UtcNow.AddDays(90);
                user.PasswordExpired = false;
                user.PasswordAttempts = 0;
                user.Locked = false;
                user.UpdatedDate = DateTime.UtcNow;
                user.LastProcessedTimeStamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Password reset successfully" });
            }
            catch
            {
                return BadRequest(new { Message = "Invalid or expired token" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var secret = GetJwtSecret();
            var key = Encoding.ASCII.GetBytes(secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("firstName", user.FirstName),
                    new Claim("lastName", user.LastName),
                    new Claim("sectionId", user.SectionId?.ToString() ?? ""),
                    new Claim("groupId", user.GroupId?.ToString() ?? ""),
                    new Claim("sessionId", user.CurrentSessionId ?? "")
                }),
                Expires = DateTime.UtcNow.AddMinutes(Math.Max(1, _security.Session.LifetimeMinutes)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}