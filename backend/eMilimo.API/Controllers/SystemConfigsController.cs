// backend/eMilimo.API/Controllers/SystemConfigsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using eMilimo.Core.Services;
using eMilimo.API.Services;

namespace eMilimo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemConfigsController : ControllerBase
    {
        private readonly AppDbContext _context; private readonly ICurrentUserService _current;

        public SystemConfigsController(AppDbContext context, ICurrentUserService current)
        {
            _context = context; _current = current;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var configs = await _context.SystemConfigs
                .Where(c => !c.ConfigKey.StartsWith("notification_contribution_") && !c.ConfigKey.StartsWith("notification_requisition_") && !c.ConfigKey.StartsWith("notification_asset_request_"))
                .Where(c => c.ConfigKey.Trim() != "license_user_identifier")
                .Where(c => c.ConfigKey.Trim() != CashDenominationSettingsService.ConfigKey)
                .OrderBy(c => c.ConfigKey)
                .Select(c => new SystemConfigDto
                {
                    Id = c.Id.ToString(),
                    ConfigKey = c.ConfigKey,
                    ConfigValue = c.ConfigValue,
                    Description = c.Description,
                    IsArchived = c.IsArchived
                })
                .ToListAsync();

            return Ok(configs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null || IsManagedKey(config.ConfigKey))
                return NotFound();

            return Ok(new SystemConfigDto
            {
                Id = config.Id.ToString(),
                ConfigKey = config.ConfigKey,
                ConfigValue = config.ConfigValue,
                Description = config.Description,
                IsArchived = config.IsArchived
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSystemConfigDto dto)
        {
            if (_current.GetUserRole() != "SuperAdmin") return Forbid();
            var key = dto.ConfigKey.Trim();
            if (IsManagedKey(key)) return BadRequest(new { Message = "This key is managed by its owning process." });
            if (await _context.SystemConfigs.AnyAsync(c => c.ConfigKey.Trim() == key))
                return BadRequest(new { Message = "Config key already exists." });

            var config = new SystemConfig
            {
                ConfigKey = key,
                ConfigValue = dto.ConfigValue,
                Description = dto.Description,
                IsArchived = false
            };

            _context.SystemConfigs.Add(config);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = config.Id }, new SystemConfigDto
            {
                Id = config.Id.ToString(),
                ConfigKey = config.ConfigKey,
                ConfigValue = config.ConfigValue,
                Description = config.Description,
                IsArchived = config.IsArchived
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSystemConfigDto dto)
        {
            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null)
                return NotFound();
            var key = dto.ConfigKey.Trim();
            if (IsManagedKey(config.ConfigKey) || IsManagedKey(key)) return BadRequest(new { Message = "This key is generated and managed by eMilimo and cannot be edited or archived." });
            var isSuperAdmin = _current.GetUserRole() == "SuperAdmin";
            if (!isSuperAdmin && (!string.Equals(config.ConfigKey, dto.ConfigKey, StringComparison.Ordinal) ||
                                  !string.Equals(config.Description ?? string.Empty, dto.Description ?? string.Empty, StringComparison.Ordinal)))
                return BadRequest(new { Message = "Only SuperAdmin can change a system configuration key or description." });
            if (await _context.SystemConfigs.AnyAsync(c => c.ConfigKey.Trim() == key && c.Id != id))
                return BadRequest(new { Message = "Config key already exists." });

            if (isSuperAdmin) config.ConfigKey = key;
            config.ConfigValue = dto.ConfigValue;
            if (isSuperAdmin) config.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new SystemConfigDto
            {
                Id = config.Id.ToString(),
                ConfigKey = config.ConfigKey,
                ConfigValue = config.ConfigValue,
                Description = config.Description,
                IsArchived = config.IsArchived
            });
        }

        [HttpPut("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            if (_current.GetUserRole() != "SuperAdmin") return Forbid();
            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null)
                return NotFound();
            if (IsManagedKey(config.ConfigKey)) return BadRequest(new { Message = "This key is managed by its owning process." });

            config.IsArchived = !config.IsArchived;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Status toggled to {(config.IsArchived ? "Archived" : "Active")}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (_current.GetUserRole() != "SuperAdmin") return Forbid();
            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null)
                return NotFound();
            if (IsManagedKey(config.ConfigKey)) return BadRequest(new { Message = "This key is managed by its owning process." });

            _context.SystemConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsManagedKey(string key) => key.Trim().Equals("license_user_identifier", StringComparison.OrdinalIgnoreCase) ||
            key.Trim().Equals(CashDenominationSettingsService.ConfigKey, StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("notification_contribution_", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("notification_requisition_", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("notification_asset_request_", StringComparison.OrdinalIgnoreCase);
    }
}
