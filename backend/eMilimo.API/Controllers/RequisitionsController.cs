using Microsoft.AspNetCore.Authorization;
using eMilimo.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using eMilimo.Core.DTOs;
using eMilimo.Core.Enums;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using eMilimo.Core.Entities.Companions;
using eMilimo.Core.Entities;

namespace eMilimo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RequisitionsController : ControllerBase
    {
        private readonly IRequisitionService _requisitionService;
        private readonly ILockService _lockService;
        private readonly ICurrentUserService _currentUserService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RequisitionsController(
            IRequisitionService requisitionService,
            ILockService lockService,
            ICurrentUserService currentUserService,
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _requisitionService = requisitionService;
            _lockService = lockService;
            _currentUserService = currentUserService;
            _context = context;
            _env = env;
        }

        private Guid GetUserId()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User not authenticated");
            return userId.Value;
        }

        // ============== GET: Stats ==============
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var userId = GetUserId();
                var stats = await _requisitionService.GetStatsAsync(userId);
                return Ok(stats);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        // ============== CREATE ==============
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequirePermission("requisitions.initiate")]
        public async Task<IActionResult> CreateRequisition([FromForm] CreateRequisitionDto dto)
        {
            try
            {
                if (_currentUserService.GetUserRole() == nameof(UserRole.Admin))
                    return StatusCode(403, new { message = "Administrators may view requisitions and reports but cannot create requisitions." });
                var userId = GetUserId();
                var requisition = await _requisitionService.CreateRequisitionAsync(dto, userId);

                return CreatedAtAction(nameof(GetRequisition), new { id = requisition.Id }, new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Amount,
                    requisition.Version
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
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        // ============== GET: By ID ==============
        [HttpGet("{id}")]
        [RequirePermission("requisitions.view")]
        public async Task<IActionResult> GetRequisition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.GetRequisitionAsync(id, userId);
                var signerIds = new[] { requisition.RequisitorId, requisition.FirstSeniorApproverId, requisition.SecondSeniorApproverId, requisition.TreasurerApproverId, requisition.AccountantApproverId, requisition.ChurchAuthorityApproverId, requisition.MinisterApproverId, requisition.ApprovedById, requisition.MoneyReadyById, requisition.CollectedById }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
                var signers = await _context.Users.Where(u => signerIds.Contains(u.Id)).Select(u => new { u.Id, u.FirstName, u.LastName, u.Role, u.SignatureData }).ToListAsync();

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.GroupId,
                    requisition.SectionId,
                    requisition.CommitteeId,
                    requisition.RequisitorId,
                    requisition.FirstSeniorApproverId,
                    requisition.SecondSeniorApproverId,
                    requisition.TreasurerApproverId,
                    requisition.AccountantApproverId,
                    requisition.ChurchAuthorityApproverId,
                    requisition.MinisterApproverId,
                    requisition.ApprovedById,
                    requisition.MoneyReadyById,
                    requisition.CollectedById,
                    requisition.Payee,
                    requisition.PaymentMethod,
                    requisition.FundSource,
                    requisition.Amount,
                    requisition.AmountWords,
                    requisition.PaymentDetails,
                    requisition.Status,
                    requisition.RejectionReason,
                    requisition.ApprovedAt,
                    requisition.MoneyReadyAt,
                    requisition.CollectedAt,
                    requisition.CompletedAt,
                    requisition.IsArchived,
                    requisition.CreatedDate,
                    requisition.UpdatedDate,
                    requisition.Version,
                    Items = requisition.Items?.Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.Quantity,
                        i.UnitPrice,
                        i.Total
                    }),
                    Group = requisition.Group != null ? new { requisition.Group.Id, requisition.Group.GroupName } : null,
                    Section = requisition.Section != null ? new { requisition.Section.Id, requisition.Section.SectionName } : null,
                    Committee = requisition.Committee != null ? new { requisition.Committee.Id, requisition.Committee.CommitteeName } : null,
                    Signers = signers,
                    Requisitor = requisition.Requisitor != null ? new
                    {
                        requisition.Requisitor.Id,
                        requisition.Requisitor.FirstName,
                        requisition.Requisitor.LastName,
                        requisition.Requisitor.Email
                    } : null
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
        }

        // ============== GET: List ==============
        [HttpGet]
        [RequirePermission("requisitions.view")]
        public async Task<IActionResult> GetRequisitions([FromQuery] RequisitionFilterDto filter)
        {
            try
            {
                var userId = GetUserId();
                var requisitions = await _requisitionService.GetRequisitionsAsync(filter, userId);

                return Ok(requisitions.Select(r => new
                {
                    r.Id,
                    r.RequisitionNumber,
                    r.Payee,
                    r.Amount,
                    r.Status,
                    r.CreatedDate,
                    r.UpdatedDate,
                    r.Version,
                    Items = r.Items?.Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.Quantity,
                        i.UnitPrice,
                        i.Total
                    }),
                    Group = r.Group != null ? new { r.Group.Id, r.Group.GroupName } : null,
                    Section = r.Section != null ? new { r.Section.Id, Name = r.Section.SectionName } : null,
                    Committee = r.Committee != null ? new { r.Committee.Id, Name = r.Committee.CommitteeName } : null,
                    Requisitor = r.Requisitor != null ? new
                    {
                        r.Requisitor.Id,
                        r.Requisitor.FirstName,
                        r.Requisitor.LastName
                    } : null
                }));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        // ============== UPDATE ==============
        [HttpPut("{id}")]
        [RequirePermission("requisitions.initiate")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateRequisition(Guid id, [FromForm] UpdateRequisitionDto dto)
        {
            try
            {
                if (_currentUserService.GetUserRole() == nameof(UserRole.Admin))
                    return StatusCode(403, new { message = "Administrators cannot edit requisitions." });
                var userId = GetUserId();
                var requisition = await _requisitionService.UpdateRequisitionAsync(id, dto, userId);

                if (dto.Files != null && dto.Files.Any())
                {
                    await SaveAttachments(id, dto.Files, userId);
                }

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Amount,
                    requisition.UpdatedDate,
                    requisition.Version,
                    Message = "Requisition updated successfully"
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var currentRequisition = await _context.Requisitions.FindAsync(id);
                return Conflict(new
                {
                    Message = "controller: " + ex.Message,
                    CurrentVersion = currentRequisition?.Version ?? 0
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
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        // ============== SUBMIT ==============
        [HttpPost("{id}/submit")]
        [RequirePermission("requisitions.initiate")]
        public async Task<IActionResult> SubmitRequisition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.SubmitRequisitionAsync(id, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Version,
                    Message = "Requisition submitted successfully"
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
        }

        // ============== APPROVE ==============
        [HttpPost("{id}/approve")]
        [RequirePermission("requisitions.approve")]
        public async Task<IActionResult> ApproveRequisition(Guid id, [FromBody] ApproveRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { Message = "User not found" });

                // Load the requisition to check its current status
                var requisition = await _context.Requisitions.FindAsync(id);
                if (requisition == null)
                    return NotFound(new { Message = "Requisition not found" });

                // Delegate to the service
                var result = await _requisitionService.ApproveRequisitionAsync(id, dto, userId);

                return Ok(new
                {
                    result.Id,
                    result.RequisitionNumber,
                    result.Status,
                    result.Version,
                    Message = "Requisition approved successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
                return StatusCode(500, new { Message = "An error occurred while approving", Details = ex.Message });
            }
        }

        // ============== REJECT ==============
        [HttpPost("{id}/reject")]
        [RequirePermission("requisitions.approve")]
        public async Task<IActionResult> RejectRequisition(Guid id, [FromBody] RejectRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { Message = "User not found" });

                var requisition = await _context.Requisitions.FindAsync(id);
                if (requisition == null)
                    return NotFound(new { Message = "Requisition not found" });

                var result = await _requisitionService.RejectRequisitionAsync(id, dto, userId);

                return Ok(new
                {
                    result.Id,
                    result.RequisitionNumber,
                    result.Status,
                    result.Version,
                    Message = "Requisition rejected"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
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
                return StatusCode(500, new { Message = "An error occurred while rejecting", Details = ex.Message });
            }
        }

        // ============== RETURN FOR CORRECTIONS ==============
        [HttpPost("{id}/return")]
        [RequirePermission("requisitions.approve")]
        public async Task<IActionResult> ReturnRequisition(Guid id, [FromBody] RejectRequisitionDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Reason))
                    return BadRequest(new { Message = "A correction reason is required." });

                var userId = GetUserId();
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return Unauthorized(new { Message = "User not found" });

                var requisition = await _context.Requisitions.FindAsync(id);
                if (requisition == null)
                    return NotFound(new { Message = "Requisition not found" });

                var result = await _requisitionService.ReturnRequisitionAsync(id, dto, userId);
                return Ok(new
                {
                    result.Id,
                    result.RequisitionNumber,
                    result.Status,
                    result.Version,
                    Message = "Requisition returned for corrections"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ============== MONEY READY ==============
        [HttpPost("{id}/money-ready")]
        [RequirePermission("requisitions.disburse")]
        public async Task<IActionResult> MarkMoneyReady(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.MarkMoneyReadyAsync(id, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Version,
                    Message = "Money marked as ready"
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
        }

        // ============== COLLECT ==============
        [HttpPost("{id}/collect")]
        [RequirePermission("requisitions.initiate")]
        public async Task<IActionResult> CollectRequisition(Guid id, [FromBody] CollectRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.CollectRequisitionAsync(id, dto, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Version,
                    Message = "Requisition collected successfully"
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
        }

        // ============== ARCHIVE ==============
        [HttpPost("{id}/archive")]
        public async Task<IActionResult> ArchiveRequisition(Guid id, [FromBody] ArchiveRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                await _requisitionService.ArchiveRequisitionAsync(id, dto, userId);

                return Ok(new { Message = "Requisition archived successfully" });
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
        }

        // ============== LOCK MANAGEMENT ==============
        [HttpPost("{id}/lock")]
        public async Task<IActionResult> LockRequisition(Guid id, [FromBody] LockRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var isLocked = await _lockService.IsLockedAsync<RequisitionLck>(id);
                if (isLocked)
                    return Conflict(new { Message = "Requisition is already locked" });

                var acquired = await _lockService.AcquireLockAsync<RequisitionLck>(
                    id,
                    userId,
                    dto.DurationSeconds ?? 300,
                    dto.Reason ?? "Editing requisition");

                if (!acquired)
                    return Conflict(new { Message = "Could not acquire lock" });

                return Ok(new
                {
                    Message = "Lock acquired successfully",
                    ExpiresAt = DateTime.UtcNow.AddSeconds(dto.DurationSeconds ?? 300)
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockRequisition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _lockService.ReleaseLockAsync<RequisitionLck>(id, userId);
                return Ok(new { Message = "Lock released successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        // ============== PRIVATE HELPERS ==============
        private async Task SaveAttachments(Guid requisitionId, List<IFormFile> files, Guid userId)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "requisitions", requisitionId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png", "image/tiff" };
            var maxFileSize = 10 * 1024 * 1024;

            foreach (var file in files)
            {
                if (!allowedTypes.Contains(file.ContentType))
                    continue;

                if (file.Length > maxFileSize)
                    continue;

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new RequisitionAtm
                {
                    Id = Guid.NewGuid(),
                    EntityId = requisitionId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/requisitions/{requisitionId}/{fileName}",
                    MimeType = file.ContentType,
                    Size = file.Length,
                    UploadedById = userId,
                    UploadedAt = DateTime.UtcNow,
                    Description = "Requisition attachment",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    LastProcessedTimeStamp = DateTime.UtcNow,
                    Version = 1
                };
                _context.RequisitionAtms.Add(attachment);
            }
            await _context.SaveChangesAsync();
        }

    }

    public class LockRequisitionDto
    {
        public int? DurationSeconds { get; set; }
        public string? Reason { get; set; }
    }
}
