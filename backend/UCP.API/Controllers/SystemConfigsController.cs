// backend/UCP.API/Controllers/SystemConfigsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.DTOs;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemConfigsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SystemConfigsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var configs = await _context.SystemConfigs
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
            if (config == null)
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

            _context.SystemConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}