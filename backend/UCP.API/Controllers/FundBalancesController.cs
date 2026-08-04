using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;
using UCP.API.Services;
using UCP.Core.Services;

namespace UCP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FundBalancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FundBalancesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/fundbalances/group/{groupId}
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetByGroup(Guid groupId)
        {
            var fiscalYear = DateTime.UtcNow.Year;
            var balance = await _context.FundBalances
                .FirstOrDefaultAsync(fb => fb.GroupId == groupId && fb.FiscalYear == fiscalYear);

            if (balance == null)
                return NotFound(new { Message = "No fund balance found for this group." });

            return Ok(new
            {
                balance.BudgetedBalance,
                balance.OwnFundsBalance,
                balance.CommittedBudgetedBalance,
                balance.CommittedOwnFundsBalance,
                AvailableBudgeted = balance.BudgetedBalance - balance.CommittedBudgetedBalance,
                AvailableOwnFunds = balance.OwnFundsBalance - balance.CommittedOwnFundsBalance
            });
        }

        [HttpGet("transactions/{entityType}/{entityId}")]
        public async Task<IActionResult> GetTransactions(string entityType, Guid entityId, [FromServices] IRolePermissionService permissions, [FromServices] ICurrentUserService current)
        {
            var type = entityType.Trim().ToLowerInvariant();
            if (type is not ("group" or "section" or "committee"))
                return BadRequest(new { Message = "Entity type must be group, section, or committee." });
            if (!await permissions.HasAsync(current.GetUserRole(), $"{type}s.manage")) return Forbid();

            var query = _context.FundTransactions
                .Include(t => t.CreatedByUser)
                .Where(t => type == "group" ? t.GroupId == entityId : type == "section" ? t.SectionId == entityId : t.CommitteeId == entityId);

            var rows = await query.OrderByDescending(t => t.CreatedDate).Take(100).Select(t => new
            {
                t.Id, t.TransactionType, t.FundType, t.Amount, t.PreviousBalance, t.NewBalance,
                t.Description, t.Source, t.ReferenceType, t.ReferenceId, t.CreatedDate, t.Metadata,
                CreatedBy = t.CreatedByUser == null ? "System" : t.CreatedByUser.FirstName + " " + t.CreatedByUser.LastName
            }).ToListAsync();

            return Ok(rows.Select(t =>
            {
                string? name = null, contentType = null, data = null;
                if (!string.IsNullOrWhiteSpace(t.Metadata)) try
                {
                    using var json = System.Text.Json.JsonDocument.Parse(t.Metadata);
                    var root = json.RootElement;
                    name = root.TryGetProperty("AttachmentName", out var n) ? n.GetString() : null;
                    contentType = root.TryGetProperty("AttachmentContentType", out var c) ? c.GetString() : null;
                    data = root.TryGetProperty("AttachmentData", out var d) ? d.GetString() : null;
                }
                catch (System.Text.Json.JsonException) { }
                return new { t.Id, t.TransactionType, t.FundType, t.Amount, t.PreviousBalance, t.NewBalance, t.Description, t.Source, t.ReferenceType, t.ReferenceId, t.CreatedDate, t.CreatedBy, AttachmentName = name, AttachmentContentType = contentType, AttachmentData = data };
            }));
        }
    }
}
