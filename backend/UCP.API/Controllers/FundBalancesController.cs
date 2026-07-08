using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;

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
    }
}