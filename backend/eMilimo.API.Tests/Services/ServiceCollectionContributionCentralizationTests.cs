using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class ServiceCollectionContributionCentralizationTests
{
    [Fact]
    public void Model_OwnsServiceCollectionLinkOnTheSourceLine()
    {
        using var db = CreateDb();
        var line = db.Model.FindEntityType(typeof(ServiceCollectionEntryLine))!;
        var contribution = db.Model.FindEntityType(typeof(Contribution))!;

        Assert.NotNull(line.FindProperty(nameof(ServiceCollectionEntryLine.ContributionId)));
        Assert.Null(contribution.FindProperty("ServiceCollectionEntryLineId"));
        var relationship = Assert.Single(line.GetForeignKeys().Where(x =>
            x.PrincipalEntityType.ClrType == typeof(Contribution)));
        Assert.True(relationship.IsUnique);
        Assert.Equal(nameof(ServiceCollectionEntryLine.ContributionId), Assert.Single(relationship.Properties).Name);
    }

    [Fact]
    public async Task CentralPosting_HandlesOrganizationWideServiceContributionExactlyOnce()
    {
        await using var db = CreateDb();
        var actor = Guid.NewGuid();
        var type = new ContributionType { Id = Guid.NewGuid(), Code = "OFFERING", Name = "Offering" };
        var source = new ContributionSource { Id = Guid.NewGuid(), Name = "Service Collection" };
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(), ContributionNumber = "CON-TEST-001", ContributionTypeId = type.Id,
            SourceId = source.Id, Amount = 125m, ContributionDate = DateTime.UtcNow,
            PaymentMethod = "SERVICE_COLLECTION", Status = "CONFIRMED", OriginType = "SERVICE_COLLECTION",
            CreatedById = actor, UpdatedById = actor
        };
        contribution.Allocations.Add(new ContributionAllocation
        {
            Id = Guid.NewGuid(), ContributionId = contribution.Id, ContributionTypeId = type.Id,
            AllocatedAmount = 125m, PercentageAllocation = 100m, IsPrimary = true,
            CreatedById = actor, UpdatedById = actor
        });
        db.AddRange(type, source, contribution);
        await db.SaveChangesAsync();
        var service = new ContributionPostingService(db);

        var first = await service.PostAsync(new ContributionPostingRequest(
            contribution.Id, actor, DateTime.UtcNow, "SERVICE_COLLECTION_APPROVAL"));
        var second = await service.PostAsync(new ContributionPostingRequest(
            contribution.Id, actor, DateTime.UtcNow, "SERVICE_COLLECTION_APPROVAL_REPLAY"));

        Assert.False(first.AlreadyPosted);
        Assert.True(second.AlreadyPosted);
        Assert.Equal(125m, await db.FundBalances.Select(x => x.OwnFundsBalance).SingleAsync());
        Assert.Equal(1, await db.FundTransactions.CountAsync(x =>
            x.ReferenceType == "CONTRIBUTION" && x.ReferenceId == contribution.Id));
    }

    [Fact]
    public async Task ServiceCollectionPosting_CreatesCentralContributionsAndIsReplaySafe()
    {
        await using var db = CreateDb();
        var actor = Guid.NewGuid();
        var source = new ContributionSource { Id = Guid.NewGuid(), Name = "Service Collection" };
        var offering = new ContributionType { Id = Guid.NewGuid(), Code = "OFFERING", Name = "Offering" };
        var tithe = new ContributionType { Id = Guid.NewGuid(), Code = "TITHE", Name = "Tithe" };
        var serviceEvent = new ServiceEvent { Id = Guid.NewGuid(), StartsAtUtc = DateTime.UtcNow.AddDays(-1) };
        var collection = new eMilimo.Core.Entities.ServiceCollection
        {
            Id = Guid.NewGuid(), CollectionNumber = "SCL-TEST-001", ContributionSourceId = source.Id,
            ServiceEventId = serviceEvent.Id, ServiceEvent = serviceEvent, Status = ServiceCollectionStatuses.Approved,
            PurposeTotal = 75m, TenderTotal = 75m, BaseCurrencyCode = "ZMW", CreatedById = actor, UpdatedById = actor
        };
        var entry = new ServiceCollectionEntry
        {
            Id = Guid.NewGuid(), ServiceCollectionId = collection.Id, PartyType = "GUEST",
            ContributorNameSnapshot = "Test Contributor", CreatedById = actor, UpdatedById = actor
        };
        entry.Lines.Add(new ServiceCollectionEntryLine
        {
            Id = Guid.NewGuid(), ServiceCollectionEntryId = entry.Id, ContributionTypeId = offering.Id,
            Amount = 25m, CreatedById = actor, UpdatedById = actor
        });
        entry.Lines.Add(new ServiceCollectionEntryLine
        {
            Id = Guid.NewGuid(), ServiceCollectionEntryId = entry.Id, ContributionTypeId = tithe.Id,
            Amount = 50m, CreatedById = actor, UpdatedById = actor
        });
        collection.Entries.Add(entry);
        db.AddRange(source, offering, tithe, serviceEvent, collection);
        await db.SaveChangesAsync();
        var service = new ServiceCollectionContributionPostingService(
            db, new SequenceReferenceService(), new ContributionPostingService(db));

        var first = await service.PostAsync(collection, actor);
        var second = await service.PostAsync(collection, actor);

        Assert.Equal(2, first.Created);
        Assert.Equal(0, second.Created);
        Assert.Equal(2, second.Replayed);
        Assert.Equal(2, await db.Contributions.CountAsync(x => x.OriginType == "SERVICE_COLLECTION"));
        Assert.Equal(2, await db.FundTransactions.CountAsync(x => x.ReferenceType == "CONTRIBUTION"));
        Assert.All(await db.ServiceCollectionEntryLines.ToListAsync(), x => Assert.NotNull(x.ContributionId));
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class SequenceReferenceService : IReferenceNumberService
    {
        private int _sequence;
        public Task<string> NextAsync(string referenceType, DateOnly? date = null, CancellationToken cancellationToken = default) =>
            Task.FromResult($"CON-TEST-{Interlocked.Increment(ref _sequence):000}");
    }
}
