using Microsoft.AspNetCore.Authorization;
using UCP.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Core.Services;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers
{
    [ApiController, Route("api/[controller]"), Authorize]
    public class CommitteesController : ControllerBase
    {
        private readonly AppDbContext _context; private readonly ICurrentUserService _currentUser;
        public CommitteesController(AppDbContext context, ICurrentUserService currentUser) { _context = context; _currentUser = currentUser; }
        private bool IsAdmin() => _currentUser.GetUserRole() is "Admin" or "SuperAdmin";

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDisabled = false)
        {
            var query = _context.Committees.AsQueryable();
            if (!IsAdmin()) { var userId = _currentUser.GetUserId(); query = query.Where(c => c.UserAssignments.Any(a => a.UserId == userId)); }
            if (!includeDisabled) query = query.Where(c => c.IsActive);
            var fiscalYear = DateTime.UtcNow.Year;
            return Ok(await query.OrderBy(c => c.CommitteeName).Select(c => new
            {
                c.Id, c.CommitteeName, c.Description, c.IsActive,
                Balance = _context.FundBalances.Where(b => b.CommitteeId == c.Id && b.FiscalYear == fiscalYear)
                    .Select(b => new { b.BudgetedBalance, b.OwnFundsBalance, b.CommittedBudgetedBalance, b.CommittedOwnFundsBalance,
                        AvailableBudgeted = b.BudgetedBalance - b.CommittedBudgetedBalance,
                        AvailableOwnFunds = b.OwnFundsBalance - b.CommittedOwnFundsBalance }).FirstOrDefault()
            }).ToListAsync());
        }

        [HttpPost, Authorize, RequirePermission("committees.manage")]
        public async Task<IActionResult> Create(CommitteeRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { Message = "Committee name is required." });
            if (await _context.Committees.AnyAsync(c => c.CommitteeName.ToLower() == dto.Name.ToLower())) return Conflict(new { Message = "Committee already exists." });
            var committee = new Committee { Id = Guid.NewGuid(), CommitteeName = dto.Name.Trim(), Description = dto.Description, IsActive = true };
            _context.Committees.Add(committee); await _context.SaveChangesAsync(); return Ok(committee);
        }

        [HttpPut("{id}"), Authorize, RequirePermission("committees.manage")]
        public async Task<IActionResult> Update(Guid id, CommitteeRequest dto)
        {
            var committee = await _context.Committees.FindAsync(id); if (committee == null) return NotFound();
            committee.CommitteeName = dto.Name.Trim(); committee.Description = dto.Description; committee.UpdatedDate = DateTime.UtcNow; committee.Version++; await _context.SaveChangesAsync(); return Ok(committee);
        }

        [HttpPost("{id}/{action}"), Authorize, RequirePermission("committees.manage")]
        public async Task<IActionResult> SetStatus(Guid id, string action)
        {
            var committee = await _context.Committees.FindAsync(id); if (committee == null) return NotFound();
            if (action != "activate" && action != "deactivate") return BadRequest();
            committee.IsActive = action == "activate"; committee.UpdatedDate = DateTime.UtcNow; committee.Version++; await _context.SaveChangesAsync(); return Ok(committee);
        }

        [HttpPost("{id}/add-funds"), Authorize, RequirePermission("committees.manage")]
        public async Task<IActionResult> AddFunds(Guid id, AddFundsDto dto)
        {
            if (dto.BudgetedAmount == 0 && dto.OwnFundsAmount == 0) return BadRequest(new { Message = "Enter a budgeted or own-fund amount." });
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest(new { Message = "A reason is required for every fund entry." });
            if (!await _context.Committees.AnyAsync(c => c.Id == id)) return NotFound();
            var year = DateTime.UtcNow.Year;
            var balance = await _context.FundBalances.FirstOrDefaultAsync(b => b.CommitteeId == id && b.FiscalYear == year);
            if (balance == null) { balance = new FundBalance { Id = Guid.NewGuid(), CommitteeId = id, FiscalYear = year }; _context.FundBalances.Add(balance); }
            if (balance.BudgetedBalance + dto.BudgetedAmount < 0 || balance.OwnFundsBalance + dto.OwnFundsAmount < 0) return BadRequest(new { Message = "Balance cannot go negative." });
            var previousBudgeted = balance.BudgetedBalance; var previousOwn = balance.OwnFundsBalance; var userId = _currentUser.GetUserId();
            balance.BudgetedBalance += dto.BudgetedAmount; balance.OwnFundsBalance += dto.OwnFundsAmount; balance.UpdatedDate = DateTime.UtcNow; balance.UpdatedById = userId; balance.Version++;
            if (dto.BudgetedAmount != 0) _context.FundTransactions.Add(CreateFundEntry(id, "BUDGETED", dto.BudgetedAmount, previousBudgeted, balance.BudgetedBalance, dto, userId));
            if (dto.OwnFundsAmount != 0) _context.FundTransactions.Add(CreateFundEntry(id, "OWN_FUNDS", dto.OwnFundsAmount, previousOwn, balance.OwnFundsBalance, dto, userId));
            await _context.SaveChangesAsync(); return Ok(balance);
        }

        private static FundTransaction CreateFundEntry(Guid committeeId, string fundType, decimal amount, decimal previous, decimal current, AddFundsDto dto, Guid? userId) => new()
        { Id = Guid.NewGuid(), CommitteeId = committeeId, TransactionType = "MANUAL_FUND_ENTRY", FundType = fundType, Amount = amount, PreviousBalance = previous, NewBalance = current, Description = dto.Reason.Trim(), Source = string.IsNullOrWhiteSpace(dto.Source) ? "Manual fund entry" : dto.Source.Trim(), ReferenceType = "MANUAL", CreatedById = userId, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Metadata = dto.AttachmentData == null ? null : System.Text.Json.JsonSerializer.Serialize(new { dto.AttachmentName, dto.AttachmentContentType, dto.AttachmentData }) };
    }
    public record CommitteeRequest(string Name, string? Description);
}
