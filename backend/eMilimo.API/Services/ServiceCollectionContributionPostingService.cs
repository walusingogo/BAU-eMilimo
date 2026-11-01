using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ServiceCollection = eMilimo.Core.Entities.ServiceCollection;

namespace eMilimo.API.Services;

public interface IServiceCollectionContributionPostingService
{
    Task<ServiceCollectionContributionPostingResult> PostAsync(ServiceCollection collection, Guid actor, CancellationToken cancellationToken = default);
}

public sealed record ServiceCollectionContributionPostingResult(int Lines, int Created, int Replayed);

public sealed class ServiceCollectionContributionPostingService(
    AppDbContext db,
    IReferenceNumberService references,
    IContributionPostingService contributionPosting) : IServiceCollectionContributionPostingService
{
    public async Task<ServiceCollectionContributionPostingResult> PostAsync(
        ServiceCollection collection, Guid actor, CancellationToken cancellationToken = default)
    {
        var lines = await db.ServiceCollectionEntryLines
            .Include(x => x.ServiceCollectionEntry)
            .Where(x => x.ServiceCollectionEntry!.ServiceCollectionId == collection.Id)
            .OrderBy(x => x.CreatedDate).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
            throw new InvalidOperationException("This Service Collection has no contribution items to post.");

        var confirmedAt = collection.ServiceEvent?.StartsAtUtc
            ?? await db.ServiceEvents.Where(x => x.Id == collection.ServiceEventId)
                .Select(x => x.StartsAtUtc).SingleAsync(cancellationToken);
        var created = 0;
        var replayed = 0;
        foreach (var line in lines)
        {
            Contribution contribution;
            var contributionCreated = false;
            if (line.ContributionId.HasValue)
            {
                contribution = await db.Contributions.Include(x => x.Allocations)
                    .SingleAsync(x => x.Id == line.ContributionId.Value, cancellationToken);
                replayed++;
            }
            else
            {
                var entry = line.ServiceCollectionEntry!;
                contribution = new Contribution
                {
                    Id = Guid.NewGuid(),
                    ContributionNumber = await references.NextAsync("CONTRIBUTION", cancellationToken: cancellationToken),
                    ContributionTypeId = line.ContributionTypeId,
                    SourceId = collection.ContributionSourceId,
                    Amount = Money(line.Amount),
                    ContributionDate = confirmedAt,
                    Description = $"Service Collection {collection.CollectionNumber}",
                    ReferenceNumber = collection.CollectionNumber,
                    PaymentMethod = "SERVICE_COLLECTION",
                    Status = ContributionStatus.CONFIRMED.ToString(),
                    OriginType = "SERVICE_COLLECTION",
                    MemberId = entry.MemberId,
                    MembershipNumberSnapshot = entry.MembershipNumberSnapshot,
                    ContributorNameSnapshot = entry.PartyType == "ANONYMOUS" ? null : entry.ContributorNameSnapshot,
                    CurrencyCode = collection.BaseCurrencyCode,
                    OriginalCurrencyAmount = Money(line.Amount),
                    ExchangeRateSnapshot = 1m,
                    ReceivedBy = collection.SubmittedById,
                    ConfirmedBy = actor,
                    ConfirmedAt = confirmedAt,
                    CreatedById = collection.SubmittedById ?? actor,
                    UpdatedById = actor,
                    CreatedDate = confirmedAt,
                    UpdatedDate = DateTime.UtcNow,
                    Version = 1
                };
                db.Contributions.Add(contribution);
                line.ContributionId = contribution.Id;
                line.UpdatedById = actor;
                line.UpdatedDate = DateTime.UtcNow;
                line.Version++;
                created++;
                contributionCreated = true;
            }

            if (contribution.Amount != Money(line.Amount) || contribution.ContributionTypeId != line.ContributionTypeId)
                throw new InvalidOperationException($"Service Collection line {line.Id} does not match its linked Contribution.");
            if (contribution.Allocations.Count == 0)
            {
                var allocation = new ContributionAllocation
                {
                    Id = Guid.NewGuid(), ContributionId = contribution.Id, ContributionTypeId = line.ContributionTypeId,
                    SectionId = collection.SectionId, GroupId = collection.GroupId, CommitteeId = collection.CommitteeId,
                    AllocatedAmount = contribution.Amount, PercentageAllocation = 100m, IsPrimary = true,
                    Notes = $"Service Collection {collection.CollectionNumber}", CreatedById = actor, UpdatedById = actor,
                    CreatedDate = confirmedAt, UpdatedDate = DateTime.UtcNow, Version = 1
                };
                contribution.Allocations.Add(allocation);
                db.ContributionAllocations.Add(allocation);
            }
            else if (Money(contribution.Allocations.Sum(x => x.AllocatedAmount)) != contribution.Amount)
                throw new InvalidOperationException($"Contribution {contribution.ContributionNumber} has invalid allocations and was not posted.");

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                var records = string.Join(", ", exception.Entries.Select(x =>
                    $"{x.Metadata.ClrType.Name}:{string.Join("/", x.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue))}"));
                throw new InvalidOperationException($"Service Collection central posting encountered a concurrent change in {records}.", exception);
            }
            await contributionPosting.PostAsync(new ContributionPostingRequest(
                contribution.Id, actor, confirmedAt,
                contributionCreated ? "SERVICE_COLLECTION_APPROVAL" : "SERVICE_COLLECTION_APPROVAL_REPLAY"), cancellationToken);
        }

        return new ServiceCollectionContributionPostingResult(lines.Count, created, replayed);
    }

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
