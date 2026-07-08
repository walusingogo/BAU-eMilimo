using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UCP.Core.Entities.Companions;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttachmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AttachmentsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // POST: api/attachments/requisition/{requisitionId}
        [HttpPost("requisition/{requisitionId}")]
        public async Task<IActionResult> UploadRequisitionAttachment(Guid requisitionId, IFormFile file)
        {
            var requisition = await _context.Requisitions.FindAsync(requisitionId);
            if (requisition == null)
                return NotFound(new { Message = "Requisition not found." });

            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "image/tiff" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { Message = "File type not allowed. Allowed: JPEG, PNG, PDF, TIFF." });

            // ----- Get current user ID safely -----
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { Message = "User not authenticated." });

            // ----- Save file to disk -----
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "requisitions", requisitionId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // ----- Save record in RequisitionAtm -----
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
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "File uploaded successfully.",
                AttachmentId = attachment.Id,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath
            });
        }

        // GET: api/attachments/requisition/{requisitionId}
        [HttpGet("requisition/{requisitionId}")]
        public async Task<IActionResult> GetRequisitionAttachments(Guid requisitionId)
        {
            var attachments = await _context.RequisitionAtms
                .Where(a => a.EntityId == requisitionId)
                .Select(a => new
                {
                    a.Id,
                    a.FileName,
                    a.FilePath,
                    a.MimeType,
                    a.Size,
                    a.UploadedAt,
                    a.Description
                })
                .ToListAsync();

            return Ok(attachments);
        }

        // DELETE: api/attachments/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteAttachment(Guid id)
        {
            var attachment = await _context.RequisitionAtms.FindAsync(id);
            if (attachment == null)
                return NotFound(new { Message = "Attachment not found." });

            // Delete file from disk
            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _context.RequisitionAtms.Remove(attachment);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Attachment deleted." });
        }
    }
}