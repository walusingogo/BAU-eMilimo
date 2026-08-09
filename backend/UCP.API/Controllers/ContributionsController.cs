using Microsoft.AspNetCore.Authorization;
using UCP.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using UCP.Core.DTOs;
using UCP.Core.Enums;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.Core.Entities;
using UCP.Core.Entities.Companions;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContributionsController : ControllerBase
    {
        [HttpGet("next-number")]
        [RequirePermission("contributions.initiate")]
        public async Task<IActionResult> GetNextContributionNumber()
            => Ok(new { contributionNumber = await _contributionService.GetNextContributionNumberAsync() });

        private readonly IContributionService _contributionService;
        private readonly ICurrentUserService _currentUserService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContributionsController> _logger;

        public ContributionsController(
            IContributionService contributionService,
            ICurrentUserService currentUserService,
            AppDbContext context,
            IWebHostEnvironment env,
            ILogger<ContributionsController> logger)
        {
            _contributionService = contributionService;
            _currentUserService = currentUserService;
            _context = context;
            _env = env;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User not authenticated");
            return userId.Value;
        }

        private static readonly JsonSerializerOptions AllocationJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static List<ContributionAllocationRequest> ParseAllocations(
            string allocationsJson)
        {
            if (string.IsNullOrWhiteSpace(allocationsJson))
            {
                throw new InvalidOperationException(
                    "The allocations payload is required.");
            }

            List<ContributionAllocationRequest>? allocations;

            try
            {
                allocations = JsonSerializer.Deserialize<
                    List<ContributionAllocationRequest>>(
                        allocationsJson,
                        AllocationJsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "The allocations payload contains invalid JSON.",
                    ex);
            }

            allocations ??= new List<ContributionAllocationRequest>();

            if (allocations.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one allocation is required.");
            }

            for (var index = 0; index < allocations.Count; index++)
            {
                var allocation = allocations[index];
                var targetCount = 0;

                if (allocation.GroupId.HasValue) targetCount++;
                if (allocation.SectionId.HasValue) targetCount++;
                if (allocation.CommitteeId.HasValue) targetCount++;

                if (targetCount == 0)
                {
                    throw new InvalidOperationException(
                        $"Allocation {index + 1} must contain a GroupId, " +
                        "SectionId, or CommitteeId.");
                }

                if (targetCount > 1)
                {
                    throw new InvalidOperationException(
                        $"Allocation {index + 1} must contain only one target.");
                }

                if (allocation.ContributionTypeId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Allocation {index + 1} requires a ContributionTypeId.");
                }

                if (allocation.AllocatedAmount <= 0)
                {
                    throw new InvalidOperationException(
                        $"Allocation {index + 1} must have an amount greater than zero.");
                }
            }

            return allocations;
        }

        // ============== CREATE ==============
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequirePermission("contributions.initiate")]
        public async Task<IActionResult> CreateContribution([FromForm] CreateContributionRequest dto)
        {
            try
            {
                dto.Allocations = ParseAllocations(dto.AllocationsJson ?? string.Empty);
                var userId = GetUserId();
                var contribution = await _contributionService.CreateContributionAsync(dto, userId);

                return CreatedAtAction(nameof(GetContribution), new { id = contribution.Id }, new
                {
                    contribution.Id,
                    contribution.ContributionNumber,
                    contribution.Status,
                    contribution.Amount,
                    contribution.Version
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contribution");
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        // ============== GET BY ID ==============
        [HttpGet("{id:guid}")]
        [RequirePermission("contributions.view")]
        public async Task<IActionResult> GetContribution(Guid id)
        {
            try
            {
                var contribution = await _contributionService.GetContributionByIdAsync(id);

                if (contribution == null)
                    return NotFound(new { Message = "Contribution not found" });

                return Ok(new
                {
                    contribution.Id,
                    contribution.ContributionNumber,
                    contribution.ContributionType,
                    contribution.Source,
                    contribution.Amount,
                    contribution.ContributionDate,
                    contribution.Description,
                    contribution.ReferenceNumber,
                    contribution.PaymentMethod,
                    contribution.Status,
                    contribution.IsSplit,
                    contribution.Version,
                    contribution.ConfirmedAt,
                    contribution.CreatedDate,
                    contribution.UpdatedDate,
                    contribution.RejectionReason,
                    contribution.ArchiveReason,
                    BankAccount = contribution.BankAccount != null ? new
                    {
                        contribution.BankAccount.Id,
                        contribution.BankAccount.BankName,
                        contribution.BankAccount.AccountName,
                        contribution.BankAccount.AccountNumber
                    } : null,
                    Allocations = contribution.Allocations?.Select(a => new
                    {
                        a.Id,
                        Group = a.Group != null ? new { a.Group.Id, a.Group.GroupName } : null,
                        a.Entity,
                        a.AllocatedAmount,
                        a.PercentageAllocation,
                        a.IsPrimary,
                        a.Notes
                    }),
                    ReceivedBy = contribution.ReceivedBy != null ? new
                    {
                        contribution.ReceivedBy.Id,
                        contribution.ReceivedBy.FirstName,
                        contribution.ReceivedBy.LastName,
                        contribution.ReceivedBy.Email
                    } : null,
                    ConfirmedBy = contribution.ConfirmedBy != null ? new
                    {
                        contribution.ConfirmedBy.Id,
                        contribution.ConfirmedBy.FirstName,
                        contribution.ConfirmedBy.LastName,
                        contribution.ConfirmedBy.Email,
                        contribution.ConfirmedBy.SignatureData
                    } : null,
                    CreatedBy = contribution.CreatedBy != null ? new
                    {
                        contribution.CreatedBy.Id,
                        contribution.CreatedBy.FirstName,
                        contribution.CreatedBy.LastName,
                        contribution.CreatedBy.Email,
                        contribution.CreatedBy.SignatureData
                    } : null,
                    Attachments = contribution.Attachments?.Select(a => new
                    {
                        a.Id,
                        a.FileName,
                        a.FilePath,
                        a.MimeType,
                        a.Size,
                        a.UploadedAt
                    })
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contribution");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== GET ALL ==============
        [HttpGet]
        [RequirePermission("contributions.view")]
        public async Task<IActionResult> GetContributions(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] Guid? groupId,
            [FromQuery] Guid? sectionId,
            [FromQuery] Guid? committeeId,
            [FromQuery] Guid? typeId,
            [FromQuery] Guid? sourceId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0)
        {
            try
            {
                var filter = new ContributionFilter
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GroupId = groupId,
                    SectionId = sectionId,
                    CommitteeId = committeeId,
                    TypeId = typeId,
                    SourceId = sourceId,
                    Status = status,
                    Search = search,
                    Limit = limit,
                    Offset = offset
                };

                var (data, total) = await _contributionService.GetContributionsAsync(filter);

                return Ok(new
                {
                    data = data.Select(c => new
                    {
                        c.Id,
                        c.ContributionNumber,
                        c.ContributionType,
                        c.Source,
                        c.Amount,
                        c.ContributionDate,
                        c.PaymentMethod,
                        c.Status,
                        c.IsSplit,
                        c.Version,
                        Allocations = c.Allocations?.Select(a => new
                        {
                            Group = a.Group != null ? new { a.Group.Id, a.Group.GroupName } : null,
                            a.Entity,
                            a.AllocatedAmount,
                            a.IsPrimary
                        }),
                        c.CreatedDate
                    }),
                    pagination = new { total, limit, offset }
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contributions");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== UPDATE ==============
        [HttpPut("{id:guid}")]
        [RequirePermission("contributions.initiate")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateContribution(
            Guid id,
            [FromForm] UpdateContributionDto dto)
        {
            try
            {
                var userId = GetUserId();

                // AllocationsJson is optional. When it is omitted, existing
                // allocations are left unchanged. When supplied, it replaces
                // the contribution's allocations through the service layer.
                if (dto.AllocationsJson != null)
                {
                    dto.Allocations = ParseAllocations(dto.AllocationsJson);

                    if (dto.Amount.HasValue)
                    {
                        var allocatedTotal = dto.Allocations
                            .Sum(allocation => allocation.AllocatedAmount);

                        if (allocatedTotal != dto.Amount.Value)
                        {
                            return BadRequest(new
                            {
                                Message =
                                    $"Allocation total {allocatedTotal:N2} must equal " +
                                    $"the contribution amount {dto.Amount.Value:N2}."
                            });
                        }
                    }
                }

                var contribution =
                    await _contributionService.UpdateContributionAsync(
                        id,
                        dto,
                        userId);

                if (dto.Files?.Any() == true)
                {
                    await _contributionService.UploadAttachmentsAsync(
                        id,
                        dto.Files,
                        userId);
                }

                return Ok(new
                {
                    contribution.Id,
                    contribution.ContributionNumber,
                    contribution.Status,
                    contribution.Amount,
                    contribution.UpdatedDate,
                    contribution.Version,
                    Message = "Contribution updated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating contribution {ContributionId}",
                    id);

                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred.",
                    Details = ex.Message
                });
            }
        }

        // ============== REJECT ==============
        [HttpPost("{id:guid}/reject")]
        [RequirePermission("contributions.confirm")]
        public async Task<IActionResult> RejectContribution(Guid id, [FromBody] RejectContributionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var contribution = await _contributionService.RejectContributionAsync(id, dto, userId);

                return Ok(new
                {
                    contribution.Id,
                    contribution.ContributionNumber,
                    contribution.Status,
                    contribution.RejectionReason,
                    contribution.Version,
                    Message = "Contribution rejected"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting contribution");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== SUBMIT (NEW) ==============
        [HttpPut("{id:guid}/submit")]
        [RequirePermission("contributions.initiate")]
        public async Task<IActionResult> SubmitContribution(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var contribution = await _contributionService.SubmitContributionAsync(id, userId);
                return Ok(new
                {
                    contribution.Id,
                    contribution.ContributionNumber,
                    contribution.Status,
                    contribution.Version,
                    Message = "Contribution submitted successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contribution");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== CONFIRM ==============
        [HttpPut("{id:guid}/confirm")]
        [RequirePermission("contributions.confirm")]
        public async Task<IActionResult> ConfirmContribution(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var contribution = await _contributionService.ConfirmContributionAsync(id, userId);

                return Ok(new
                {
                    contribution.Id,
                    contribution.ContributionNumber,
                    contribution.Status,
                    contribution.Version,
                    Message = "Contribution confirmed and funds allocated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming contribution");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== ARCHIVE ==============
        [HttpPost("{id:guid}/archive")]
        [Authorize]
        public async Task<IActionResult> ArchiveContribution(Guid id, [FromBody] ArchiveContributionDto dto)
        {
            try
            {
                var userId = GetUserId();
                await _contributionService.ArchiveContributionAsync(id, dto, userId);

                return Ok(new { Message = "Contribution archived successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving contribution");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== DASHBOARD STATS ==============
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string period = "month")
        {
            try
            {
                var stats = await _contributionService.GetDashboardStatsAsync(period);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== TYPES ==============
        [HttpGet("types")]
        public async Task<IActionResult> GetContributionTypes()
        {
            try
            {
                var types = await _contributionService.GetContributionTypesAsync();
                return Ok(new { data = types });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contribution types");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPost("types")]
        [RequirePermission("contributions.admin")]
        public async Task<IActionResult> CreateContributionType([FromBody] CreateContributionTypeDto dto)
        {
            try
            {
                if (await _context.ContributionTypes.AnyAsync(t => t.Code == dto.Code))
                    return BadRequest(new { Message = "Code already exists." });

                var type = new ContributionType
                {
                    Code = dto.Code,
                    Name = dto.Name,
                    Description = dto.Description,
                    IsActive = true
                };

                _context.ContributionTypes.Add(type);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContributionTypes), new { id = type.Id }, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contribution type");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPut("types/{id}")]
        [RequirePermission("contributions.admin")]
        public async Task<IActionResult> UpdateContributionType(Guid id, [FromBody] UpdateContributionTypeDto dto)
        {
            var type = await _context.ContributionTypes.FindAsync(id);
            if (type == null)
                return NotFound(new { Message = "Type not found" });

            if (await _context.ContributionTypes.AnyAsync(t => t.Code == dto.Code && t.Id != id))
                return BadRequest(new { Message = "Code already exists." });

            type.Code = dto.Code;
            type.Name = dto.Name;
            type.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(type);
        }

        [HttpPut("types/{id}/toggle-status")]
        [RequirePermission("contributions.admin")]
        public async Task<IActionResult> ToggleTypeStatus(Guid id)
        {
            try
            {
                var type = await _context.ContributionTypes.FindAsync(id);
                if (type == null)
                    return NotFound(new { Message = "Type not found" });

                type.IsActive = !type.IsActive;
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Status toggled to {(type.IsActive ? "Active" : "Inactive")}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling type status");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== SOURCES ==============
        [HttpGet("sources")]
        public async Task<IActionResult> GetContributionSources()
        {
            try
            {
                var sources = await _contributionService.GetContributionSourcesAsync();
                return Ok(new { data = sources });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contribution sources");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPost("sources")]
        [RequirePermission("contributions.admin")]
        public async Task<IActionResult> CreateContributionSource([FromBody] CreateContributionSourceDto dto)
        {
            try
            {
                if (await _context.ContributionSources.AnyAsync(s => s.Name == dto.Name))
                    return BadRequest(new { Message = "Source name already exists." });

                var source = new ContributionSource
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    IsActive = true
                };

                _context.ContributionSources.Add(source);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContributionSources), new { id = source.Id }, source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contribution source");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPut("sources/{id}")]
        [RequirePermission("contributions.admin")]
        public async Task<IActionResult> UpdateContributionSource(Guid id, [FromBody] UpdateContributionSourceDto dto)
        {
            var source = await _context.ContributionSources.FindAsync(id);
            if (source == null)
                return NotFound(new { Message = "Source not found" });

            if (await _context.ContributionSources.AnyAsync(s => s.Name == dto.Name && s.Id != id))
                return BadRequest(new { Message = "Name already exists." });

            source.Name = dto.Name;
            source.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok(source);
        }

        [HttpPut("sources/{id}/toggle-status")]
        [RequirePermission("contributions.admin")]
        public async Task<IActionResult> ToggleSourceStatus(Guid id)
        {
            try
            {
                var source = await _context.ContributionSources.FindAsync(id);
                if (source == null)
                    return NotFound(new { Message = "Source not found" });

                source.IsActive = !source.IsActive;
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Status toggled to {(source.IsActive ? "Active" : "Inactive")}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling source status");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ============== ATTACHMENTS ==============
        [HttpPost("{id:guid}/attachments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAttachments(Guid id, List<IFormFile> files)
        {
            try
            {
                var userId = GetUserId();
                
                var contribution = await _contributionService.GetContributionByIdAsync(id);
                if (contribution == null)
                    return NotFound(new { error = "Contribution not found" });

                if (files == null || !files.Any())
                    return BadRequest(new { error = "No files provided" });

                var result = await _contributionService.UploadAttachmentsAsync(id, files, userId);

                return Ok(new
                {
                    message = "Attachments uploaded successfully",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachments");
                return StatusCode(500, new { error = "An error occurred while uploading attachments" });
            }
        }

        [HttpGet("{id:guid}/attachments")]
        public async Task<IActionResult> GetAttachments(Guid id)
        {
            try
            {
                var contribution = await _contributionService.GetContributionByIdAsync(id);
                if (contribution == null)
                    return NotFound(new { error = "Contribution not found" });

                var attachments = await _contributionService.GetAttachmentsAsync(id);

                return Ok(new { data = attachments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachments");
                return StatusCode(500, new { error = "An error occurred while retrieving attachments" });
            }
        }

        [HttpDelete("{id:guid}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
        {
            try
            {
                var userId = GetUserId();
                
                var contribution = await _contributionService.GetContributionByIdAsync(id);
                if (contribution == null)
                    return NotFound(new { error = "Contribution not found" });

                await _contributionService.DeleteAttachmentAsync(id, attachmentId, userId);

                return Ok(new { message = "Attachment deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment");
                return StatusCode(500, new { error = "An error occurred while deleting the attachment" });
            }
        }
    }
}
