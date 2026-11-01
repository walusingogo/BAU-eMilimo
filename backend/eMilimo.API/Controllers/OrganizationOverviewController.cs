using eMilimo.API.Authorization;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Controllers;

[ApiController]
[Route("api/organization-overview")]
[Authorize]
[RequirePermission("reports.view")]
public sealed class OrganizationOverviewController(
    IOrganizationOverviewService overview,
    ICurrentUserService currentUser,
    AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await overview.GetOrganizationOverviewAsync(cancellationToken));

    [HttpGet("organization-wide")]
    public async Task<IActionResult> OrganizationWide(CancellationToken cancellationToken)
    {
        var result = await overview.GetOrganizationWideFinancialsAsync(cancellationToken);
        return Ok(new { total = result.Total, byType = result.ByType });
    }

    [HttpGet("sections")]
    public async Task<IActionResult> Sections(CancellationToken cancellationToken) =>
        Ok(await overview.GetAllSectionsFinancialViewAsync(cancellationToken));

    [HttpGet("sections/{id:guid}")]
    public async Task<IActionResult> Section(Guid id, CancellationToken cancellationToken)
    {
        var result = await overview.GetSectionFinancialViewAsync(id, cancellationToken);
        return result is null ? NotFound(new { message = "Section was not found." }) : Ok(result);
    }

    [HttpGet("groups")]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken) =>
        Ok(await overview.GetAllGroupsFinancialViewAsync(cancellationToken));

    [HttpGet("groups/{id:guid}")]
    public async Task<IActionResult> Group(Guid id, CancellationToken cancellationToken)
    {
        var result = await overview.GetGroupFinancialViewAsync(id, cancellationToken);
        return result is null ? NotFound(new { message = "Group was not found." }) : Ok(result);
    }

    [HttpGet("committees")]
    public async Task<IActionResult> Committees(CancellationToken cancellationToken) =>
        Ok(await overview.GetAllCommitteesFinancialViewAsync(cancellationToken));

    [HttpGet("committees/{id:guid}")]
    public async Task<IActionResult> Committee(Guid id, CancellationToken cancellationToken)
    {
        var result = await overview.GetCommitteeFinancialViewAsync(id, cancellationToken);
        return result is null ? NotFound(new { message = "Committee was not found." }) : Ok(result);
    }

    [HttpPost("refresh")]
    [RequirePermission("settings.manage")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var actor = currentUser.GetUserId() ?? throw new UnauthorizedAccessException();
        await overview.RefreshOrganizationOverviewAsync(actor, cancellationToken);
        return Ok(await overview.GetOrganizationOverviewAsync(cancellationToken));
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> Ledger(
        [FromQuery] string scopeType = "ORGANIZATION",
        [FromQuery] Guid? scopeId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        scopeType = scopeType.Trim().ToUpperInvariant();
        var transactions = db.FundTransactions.AsNoTracking().AsQueryable();
        var balances = db.FundBalances.AsNoTracking().AsQueryable();
        switch (scopeType)
        {
            case "ORGANIZATION":
                transactions = transactions.Where(x => x.SectionId == null && x.GroupId == null && x.CommitteeId == null);
                balances = balances.Where(x => x.SectionId == null && x.GroupId == null && x.CommitteeId == null);
                break;
            case "SECTION" when scopeId.HasValue:
                transactions = transactions.Where(x => x.SectionId == scopeId);
                balances = balances.Where(x => x.SectionId == scopeId);
                break;
            case "GROUP" when scopeId.HasValue:
                transactions = transactions.Where(x => x.GroupId == scopeId);
                balances = balances.Where(x => x.GroupId == scopeId);
                break;
            case "COMMITTEE" when scopeId.HasValue:
                transactions = transactions.Where(x => x.CommitteeId == scopeId);
                balances = balances.Where(x => x.CommitteeId == scopeId);
                break;
            default:
                return BadRequest(new { message = "A valid scope type and identifier are required." });
        }

        var rows = await transactions.OrderByDescending(x => x.CreatedDate).Take(Math.Clamp(limit, 1, 500))
            .Select(x => new
            {
                x.Id, x.TransactionType, x.FundType, x.Amount, x.PreviousBalance, x.NewBalance,
                x.Description, x.Source, x.ReferenceType, x.ReferenceId, x.IsReversed,
                x.ReversedAt, x.CreatedDate
            }).ToListAsync(cancellationToken);
        var balance = await balances.SumAsync(x => x.OwnFundsBalance + x.BudgetedBalance, cancellationToken);
        var available = await balances.SumAsync(x => x.OwnFundsBalance + x.BudgetedBalance - x.CommittedOwnFundsBalance - x.CommittedBudgetedBalance, cancellationToken);
        return Ok(new
        {
            scopeType, scopeId, balance, availableBalance = available,
            inflows = rows.Where(x => !x.IsReversed && x.Amount > 0).Sum(x => x.Amount),
            outflows = Math.Abs(rows.Where(x => !x.IsReversed && x.Amount < 0).Sum(x => x.Amount)),
            reversals = rows.Where(x => x.IsReversed).Sum(x => Math.Abs(x.Amount)),
            transactions = rows
        });
    }
}
