using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using UCP.Core.DTOs;
using UCP.Core.Enums;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.Core.Entities.Companions;

namespace UCP.API.Controllers
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

        // ============== POST: Create with files ==============
        [HttpPost("with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateRequisitionWithFiles([FromForm] CreateRequisitionWithFilesDto dto)
        {
            try
            {
                var requisitionDto = new CreateRequisitionDto
                {
                    GroupId = dto.GroupId,
                    Payee = dto.Payee,
                    PaymentMethod = dto.PaymentMethod,
                    FundSource = dto.FundSource,
                    PaymentDetails = dto.PaymentDetails,
                    Items = dto.Items
                };

                var userId = GetUserId();
                var requisition = await _requisitionService.CreateRequisitionAsync(requisitionDto, userId);

                if (dto.Files != null && dto.Files.Any())
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "requisitions", requisition.Id.ToString());
                    Directory.CreateDirectory(uploadsFolder);

                    var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png" };
                    foreach (var file in dto.Files)
                    {
                        if (!allowedTypes.Contains(file.ContentType))
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
                            EntityId = requisition.Id,
                            FileName = file.FileName,
                            FilePath = $"/uploads/requisitions/{requisition.Id}/{fileName}",
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

                return CreatedAtAction(nameof(GetRequisition), new { id = requisition.Id }, new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Amount
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
        // ============== Standard CRUD ==============

        [HttpPost]
        public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.CreateRequisitionAsync(dto, userId);
                return CreatedAtAction(nameof(GetRequisition), new { id = requisition.Id }, new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Amount
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequisition(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.GetRequisitionAsync(id, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.GroupId,
                    requisition.RequisitorId,
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
                    Items = requisition.Items?.Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.Quantity,
                        i.UnitPrice,
                        i.Total
                    }),
                    Group = requisition.Group != null ? new { requisition.Group.Id, requisition.Group.GroupName } : null,
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

        [HttpGet]
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
                    Items = r.Items?.Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.Quantity,
                        i.UnitPrice,
                        i.Total
                    }),
                    Group = r.Group != null ? new { r.Group.Id, r.Group.GroupName } : null,
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequisition(Guid id, [FromBody] UpdateRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.UpdateRequisitionAsync(id, dto, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
                    requisition.Amount,
                    requisition.UpdatedDate
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

        [HttpPost("{id}/submit")]
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

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveRequisition(Guid id, [FromBody] ApproveRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.ApproveRequisitionAsync(id, dto, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
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
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectRequisition(Guid id, [FromBody] RejectRequisitionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var requisition = await _requisitionService.RejectRequisitionAsync(id, dto, userId);

                return Ok(new
                {
                    requisition.Id,
                    requisition.RequisitionNumber,
                    requisition.Status,
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
        }

        [HttpPost("{id}/money-ready")]
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

        [HttpPost("{id}/collect")]
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
    }
}