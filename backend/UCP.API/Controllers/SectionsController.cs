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
    public class SectionsController : ControllerBase
    {
        private readonly AppDbContext _context; private readonly ICurrentUserService _currentUser;
        public SectionsController(AppDbContext context, ICurrentUserService currentUser) { _context = context; _currentUser = currentUser; }
        private bool IsAdmin() => _currentUser.GetUserRole() is "Admin" or "SuperAdmin";

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDisabled = false)
        {
            var query = _context.Sections.AsQueryable();
            if (!IsAdmin()) { var userId = _currentUser.GetUserId(); query = query.Where(s => _context.Users.Any(u => u.Id == userId && u.SectionId == s.Id)); }
            if (!includeDisabled) query = query.Where(s => s.IsActive);
            var fiscalYear = DateTime.UtcNow.Year;
            return Ok(await query.OrderBy(s => s.SectionNumber).Select(s => new
            {
                s.Id, s.SectionNumber, s.SectionName, s.Description, s.IsActive,
                Balance = _context.FundBalances.Where(b => b.SectionId == s.Id && b.FiscalYear == fiscalYear)
                    .Select(b => new { b.BudgetedBalance, b.OwnFundsBalance, b.CommittedBudgetedBalance, b.CommittedOwnFundsBalance,
                        AvailableBudgeted = b.BudgetedBalance - b.CommittedBudgetedBalance,
                        AvailableOwnFunds = b.OwnFundsBalance - b.CommittedOwnFundsBalance }).FirstOrDefault()
            }).ToListAsync());
        }

        [HttpPost, Authorize, RequirePermission("sections.manage")]
        public async Task<IActionResult> Create(SectionRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { Message = "Section name is required." });
            if (await _context.Sections.AnyAsync(s => s.SectionNumber == dto.Number || s.SectionName.ToLower() == dto.Name.ToLower()))
                return Conflict(new { Message = "A section with this number or name already exists." });
            var section = new Section { Id = Guid.NewGuid(), SectionNumber = dto.Number, SectionName = dto.Name.Trim(), Description = dto.Description, IsActive = true };
            _context.Sections.Add(section); await _context.SaveChangesAsync(); return Ok(section);
        }

        [HttpPut("{id}"), Authorize, RequirePermission("sections.manage")]
        public async Task<IActionResult> Update(Guid id, SectionRequest dto)
        {
            var section = await _context.Sections.FindAsync(id); if (section == null) return NotFound();
            if (await _context.Sections.AnyAsync(s => s.Id != id && (s.SectionNumber == dto.Number || s.SectionName.ToLower() == dto.Name.ToLower())))
                return Conflict(new { Message = "A section with this number or name already exists." });
            section.SectionNumber = dto.Number; section.SectionName = dto.Name.Trim(); section.Description = dto.Description; section.UpdatedDate = DateTime.UtcNow; section.Version++;
            await _context.SaveChangesAsync(); return Ok(section);
        }

        [HttpPost("{id}/{action}"), Authorize, RequirePermission("sections.manage")]
        public async Task<IActionResult> SetStatus(Guid id, string action)
        {
            var section = await _context.Sections.FindAsync(id); if (section == null) return NotFound();
            if (action != "activate" && action != "deactivate") return BadRequest();
            section.IsActive = action == "activate"; section.UpdatedDate = DateTime.UtcNow; section.Version++; await _context.SaveChangesAsync(); return Ok(section);
        }

        [HttpPost("{id}/add-funds"), Authorize, RequirePermission("sections.manage")]
        public async Task<IActionResult> AddFunds(Guid id, AddFundsDto dto)
        {
            if (dto.BudgetedAmount == 0 && dto.OwnFundsAmount == 0) return BadRequest(new { Message = "Enter a budgeted or own-fund amount." });
            if (string.IsNullOrWhiteSpace(dto.Reason)) return BadRequest(new { Message = "A reason is required for every fund entry." });
            if (!await _context.Sections.AnyAsync(s => s.Id == id)) return NotFound();
            var year = DateTime.UtcNow.Year;
            var balance = await _context.FundBalances.FirstOrDefaultAsync(b => b.SectionId == id && b.FiscalYear == year);
            if (balance == null) { balance = new FundBalance { Id = Guid.NewGuid(), SectionId = id, FiscalYear = year }; _context.FundBalances.Add(balance); }
            if (balance.BudgetedBalance + dto.BudgetedAmount < 0 || balance.OwnFundsBalance + dto.OwnFundsAmount < 0) return BadRequest(new { Message = "Balance cannot go negative." });
            var previousBudgeted = balance.BudgetedBalance; var previousOwn = balance.OwnFundsBalance; var userId = _currentUser.GetUserId();
            balance.BudgetedBalance += dto.BudgetedAmount; balance.OwnFundsBalance += dto.OwnFundsAmount; balance.UpdatedDate = DateTime.UtcNow; balance.UpdatedById = userId; balance.Version++;
            if (dto.BudgetedAmount != 0) _context.FundTransactions.Add(CreateFundEntry(id, "BUDGETED", dto.BudgetedAmount, previousBudgeted, balance.BudgetedBalance, dto, userId));
            if (dto.OwnFundsAmount != 0) _context.FundTransactions.Add(CreateFundEntry(id, "OWN_FUNDS", dto.OwnFundsAmount, previousOwn, balance.OwnFundsBalance, dto, userId));
            await _context.SaveChangesAsync(); return Ok(balance);
        }

        private static FundTransaction CreateFundEntry(Guid sectionId, string fundType, decimal amount, decimal previous, decimal current, AddFundsDto dto, Guid? userId) => new()
        { Id = Guid.NewGuid(), SectionId = sectionId, TransactionType = "MANUAL_FUND_ENTRY", FundType = fundType, Amount = amount, PreviousBalance = previous, NewBalance = current, Description = dto.Reason.Trim(), Source = string.IsNullOrWhiteSpace(dto.Source) ? "Manual fund entry" : dto.Source.Trim(), ReferenceType = "MANUAL", CreatedById = userId, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Metadata = dto.AttachmentData == null ? null : System.Text.Json.JsonSerializer.Serialize(new { dto.AttachmentName, dto.AttachmentContentType, dto.AttachmentData }) };
    }
    public record SectionRequest(int Number, string Name, string? Description);
}
