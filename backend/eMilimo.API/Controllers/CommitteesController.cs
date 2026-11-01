using Microsoft.AspNetCore.Authorization;
using eMilimo.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers
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
                c.Id, c.CommitteeName, c.Description, c.IsActive, c.ParentSectionId, c.ParentGroupId,
                ParentSectionName = c.ParentSection == null ? null : c.ParentSection.SectionName,
                ParentGroupName = c.ParentGroup == null ? null : c.ParentGroup.GroupName,
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
            var parentError = await ValidateParentAsync(dto.ParentSectionId, dto.ParentGroupId); if (parentError != null) return BadRequest(new { Message = parentError });
            if (await _context.Committees.AnyAsync(c => c.CommitteeName.ToLower() == dto.Name.ToLower())) return Conflict(new { Message = "Committee already exists." });
            var committee = new Committee { Id = Guid.NewGuid(), CommitteeName = dto.Name.Trim(), Description = dto.Description,
                ParentSectionId = dto.ParentSectionId, ParentGroupId = dto.ParentGroupId, IsActive = true };
            _context.Committees.Add(committee); await _context.SaveChangesAsync(); return Ok(committee);
        }

        [HttpPut("{id}"), Authorize, RequirePermission("committees.manage")]
        public async Task<IActionResult> Update(Guid id, CommitteeRequest dto)
        {
            var committee = await _context.Committees.FindAsync(id); if (committee == null) return NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { Message = "Committee name is required." });
            var parentError = await ValidateParentAsync(dto.ParentSectionId, dto.ParentGroupId); if (parentError != null) return BadRequest(new { Message = parentError });
            committee.CommitteeName = dto.Name.Trim(); committee.Description = dto.Description;
            committee.ParentSectionId = dto.ParentSectionId; committee.ParentGroupId = dto.ParentGroupId;
            committee.UpdatedDate = DateTime.UtcNow; committee.Version++; await _context.SaveChangesAsync(); return Ok(committee);
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

        private async Task<string?> ValidateParentAsync(Guid? sectionId, Guid? groupId)
        {
            if (sectionId.HasValue && groupId.HasValue) return "A Committee may belong to a Section or a Group, but not both.";
            if (sectionId.HasValue && !await _context.Sections.AnyAsync(x => x.Id == sectionId && x.IsActive)) return "The selected Section does not exist or is inactive.";
            if (groupId.HasValue && !await _context.Groups.AnyAsync(x => x.Id == groupId && x.IsActive && x.GroupType == "ChurchGroup")) return "The selected Group does not exist or is inactive.";
            return null;
        }
    }
    public record CommitteeRequest(string Name, string? Description, Guid? ParentSectionId = null, Guid? ParentGroupId = null);
}
