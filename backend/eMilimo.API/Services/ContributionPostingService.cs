using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

/// <summary>
/// Posts validated contributions into the financial ledger.
/// Handles organization-wide, section, group, and committee allocations.
/// </summary>
public sealed class ContributionPostingService : IContributionPostingService
{
    private readonly AppDbContext _db;

    public ContributionPostingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ContributionPostingResult> PostAsync(
        ContributionPostingRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get contribution with allocations
        var contribution = await _db.Contributions
            .Include(x => x.Allocations)
            .Include(x => x.ContributionType)
            .Include(x => x.Source)
            .FirstOrDefaultAsync(x => x.Id == request.ContributionId, cancellationToken)
            ?? throw new InvalidOperationException($"Contribution {request.ContributionId} not found.");

        // Support historical records that used the non-domain POSTED value.
        if (contribution.Status == "POSTED")
            return new ContributionPostingResult(
                request.ContributionId,
                AlreadyPosted: true,
                LedgerEntryCount: 0,
                PostedAmount: contribution.Amount);

        // Ledger existence, rather than a second contribution workflow status,
        // is the authoritative idempotency boundary for financial posting.
        var alreadyPosted = await _db.FundTransactions.AnyAsync(
            x => x.ReferenceType == "CONTRIBUTION" &&
                 x.ReferenceId == contribution.Id &&
                 !x.IsReversed,
            cancellationToken);
        if (alreadyPosted)
            return new ContributionPostingResult(
                request.ContributionId,
                AlreadyPosted: true,
                LedgerEntryCount: 0,
                PostedAmount: contribution.Amount);

        if (contribution.Status != "CONFIRMED")
            throw new InvalidOperationException($"Contribution status '{contribution.Status}' cannot be posted. Must be CONFIRMED.");

        if (!contribution.Allocations.Any())
            throw new InvalidOperationException($"Contribution {request.ContributionId} has no allocations.");

        // Verify allocations sum to contribution amount
        var allocationSum = contribution.Allocations.Sum(x => x.AllocatedAmount);
        if (allocationSum != contribution.Amount)
            throw new InvalidOperationException(
                $"Allocation total {allocationSum:C} does not match contribution amount {contribution.Amount:C}.");

        // Create fund transactions for each allocation
        var fundTransactions = new List<FundTransaction>();
        var now = request.ConfirmedAtUtc;
        var currentYear = contribution.ContributionDate.Year;

        foreach (var allocation in contribution.Allocations)
        {
            var balance = _db.FundBalances.Local.FirstOrDefault(x =>
                x.FiscalYear == currentYear &&
                x.GroupId == allocation.GroupId &&
                x.SectionId == allocation.SectionId &&
                x.CommitteeId == allocation.CommitteeId)
                ?? await _db.FundBalances.FirstOrDefaultAsync(x =>
                    x.FiscalYear == currentYear &&
                    x.GroupId == allocation.GroupId &&
                    x.SectionId == allocation.SectionId &&
                    x.CommitteeId == allocation.CommitteeId,
                    cancellationToken);

            if (balance is null)
            {
                balance = new FundBalance
                {
                    Id = Guid.NewGuid(),
                    FiscalYear = currentYear,
                    GroupId = allocation.GroupId,
                    SectionId = allocation.SectionId,
                    CommitteeId = allocation.CommitteeId,
                    CreatedById = request.ActorUserId,
                    UpdatedById = request.ActorUserId,
                    CreatedDate = now,
                    UpdatedDate = now,
                    Version = 1
                };
                _db.FundBalances.Add(balance);
            }

            var previousBalance = balance.OwnFundsBalance;
            balance.OwnFundsBalance += allocation.AllocatedAmount;
            balance.UpdatedById = request.ActorUserId;
            balance.UpdatedDate = now;
            balance.Version++;
            
            var fundTransaction = new FundTransaction
            {
                Id = Guid.NewGuid(),
                TransactionType = "ADD",
                FundType = "OWN_FUNDS",
                Amount = allocation.AllocatedAmount,
                PreviousBalance = previousBalance,
                NewBalance = balance.OwnFundsBalance,
                Description = $"Contribution: {contribution.ContributionNumber} - {contribution.ContributionType?.Name ?? "Unknown"}",
                Source = contribution.Source?.Name ?? "Unknown",
                ReferenceType = "CONTRIBUTION",
                ReferenceId = contribution.Id,
                PostingKey = $"CONTRIBUTION:{contribution.Id:N}:{allocation.Id:N}",
                GroupId = allocation.GroupId,
                SectionId = allocation.SectionId,
                CommitteeId = allocation.CommitteeId,
                IsReversed = false,
                CreatedById = request.ActorUserId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Version = 1
            };

            fundTransactions.Add(fundTransaction);
            _db.FundTransactions.Add(fundTransaction);
        }

        // CONFIRMED is the final contribution workflow status. Financial
        // posting is represented by immutable fund transactions, not by an
        // unsupported extra status value.
        contribution.ConfirmedAt = request.ConfirmedAtUtc;
        contribution.ConfirmedBy = request.ActorUserId;
        contribution.UpdatedDate = DateTime.UtcNow;
        contribution.UpdatedById = request.ActorUserId;
        contribution.Version++;

        // Invalidate the cached organization roll-up in the same unit of work.
        // The next overview request will rebuild it from authoritative posted
        // contribution allocations, preventing stale dashboard totals.
        var organizationOverview = await _db.OrganizationOverviews
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (organizationOverview is not null)
        {
            organizationOverview.IsStale = true;
            organizationOverview.UpdatedDate = DateTime.UtcNow;
            organizationOverview.UpdatedById = request.ActorUserId;
            organizationOverview.Version++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ContributionPostingResult(
            request.ContributionId,
            AlreadyPosted: false,
            LedgerEntryCount: fundTransactions.Count,
            PostedAmount: contribution.Amount);
    }

}
