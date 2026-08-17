// backend/UCP.API/Controllers/SystemConfigsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;
using UCP.Core.Services;

namespace UCP.API.Controllers
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
                .Where(c => c.ConfigKey != "license_xml" && c.ConfigKey != "license_xml_pending" && c.ConfigKey != "license_installed_at")
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
            if (config == null || IsProtectedLicenseKey(config.ConfigKey))
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
            if (IsProtectedLicenseKey(dto.ConfigKey)) return BadRequest(new { Message = "This key is managed by the licensing service." });
            if (dto.ConfigKey.Equals("license_user_identifier", StringComparison.OrdinalIgnoreCase) && _current.GetUserRole() != "SuperAdmin") return Forbid();
            if (await _context.SystemConfigs.AnyAsync(c => c.ConfigKey == dto.ConfigKey))
                return BadRequest(new { Message = "Config key already exists." });

            var config = new SystemConfig
            {
                ConfigKey = dto.ConfigKey,
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
            if (IsProtectedLicenseKey(config.ConfigKey) || IsProtectedLicenseKey(dto.ConfigKey)) return BadRequest(new { Message = "This key is managed by the licensing service." });
            if ((config.ConfigKey.Equals("license_user_identifier", StringComparison.OrdinalIgnoreCase) || dto.ConfigKey.Equals("license_user_identifier", StringComparison.OrdinalIgnoreCase)) && _current.GetUserRole() != "SuperAdmin") return Forbid();

            if (await _context.SystemConfigs.AnyAsync(c => c.ConfigKey == dto.ConfigKey && c.Id != id))
                return BadRequest(new { Message = "Config key already exists." });

            config.ConfigKey = dto.ConfigKey;
            config.ConfigValue = dto.ConfigValue;
            config.Description = dto.Description;

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
            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null)
                return NotFound();
            if (IsProtectedLicenseKey(config.ConfigKey)) return BadRequest(new { Message = "This key is managed by the licensing service." });

            config.IsArchived = !config.IsArchived;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Status toggled to {(config.IsArchived ? "Archived" : "Active")}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null)
                return NotFound();
            if (IsProtectedLicenseKey(config.ConfigKey)) return BadRequest(new { Message = "This key is managed by the licensing service." });

            _context.SystemConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsProtectedLicenseKey(string key) => key.Equals("license_xml", StringComparison.OrdinalIgnoreCase) || key.Equals("license_xml_pending", StringComparison.OrdinalIgnoreCase) || key.Equals("license_installed_at", StringComparison.OrdinalIgnoreCase);
    }
}
