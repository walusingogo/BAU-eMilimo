using eMilimo.API.Services;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class FinancialRecordBookServiceTests
{
    [Fact]
    public void ModelStatusLength_AccommodatesEveryWorkflowStatus()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new AppDbContext(options);
        var maxLength = db.Model.FindEntityType(typeof(FinancialRecordBook))!
            .FindProperty(nameof(FinancialRecordBook.Status))!.GetMaxLength();

        Assert.NotNull(maxLength);
        Assert.All(new[] { FinancialRecordBookStatuses.Draft, FinancialRecordBookStatuses.AwaitingVerification,
            FinancialRecordBookStatuses.Finalized, FinancialRecordBookStatuses.Returned, FinancialRecordBookStatuses.Archived },
            status => Assert.True(status.Length <= maxLength));
    }

    [Fact]
    public async Task UpdateAsync_AddsAContributionToAnExistingDraft()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var actorId = Guid.NewGuid();
        var type = new ContributionType { Id = Guid.NewGuid(), Code = "WEEKLY", Name = "Weekly Offering" };
        var source = new ContributionSource { Id = Guid.NewGuid(), Name = "Cash" };
        var existing = Contribution("CON-001", 100m, type.Id, source.Id);
        var added = Contribution("CON-002", 250m, type.Id, source.Id);
        var frb = new FinancialRecordBook
        {
            Id = Guid.NewGuid(), FrbNumber = "FRB-TEST-001", PeriodStart = new DateOnly(2026, 9, 28),
            PeriodEnd = new DateOnly(2026, 10, 4), Status = FinancialRecordBookStatuses.Draft,
            CreatedById = actorId, UpdatedById = actorId,
            Contributions = [new FinancialRecordBookContribution { Id = Guid.NewGuid(), ContributionId = existing.Id, CreatedById = actorId, UpdatedById = actorId }]
        };
        db.AddRange(type, source, existing, added, frb);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new FinancialRecordBookService(db, new UnusedReferences(), new GlobalAccess(), new TestCurrentUser(actorId), new WorkflowAuthorizationService(db));
        await service.UpdateAsync(frb.Id,
            new SaveFinancialRecordBookRequest(new DateTime(2026, 9, 28), new DateTime(2026, 10, 4), null, null, [existing.Id, added.Id]),
            actorId, CancellationToken.None);

        db.ChangeTracker.Clear();
        var links = await db.FinancialRecordBookContributions.Where(x => x.FinancialRecordBookId == frb.Id).ToListAsync();
        Assert.Equal(2, links.Count);
        Assert.Contains(links, x => x.ContributionId == added.Id);
    }

    [Fact]
    public async Task VerifyAsync_StewardshipConvenerCanReviewAnFrbFromAnotherSection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var homeSection = new Section { Id = Guid.NewGuid(), SectionNumber = 1, SectionName = "Section 1" };
        var sourceSection = new Section { Id = Guid.NewGuid(), SectionNumber = 2, SectionName = "Section 2" };
        var actor = new User
        {
            Id = Guid.NewGuid(), FirstName = "Stella", LastName = "Convener", Email = "convener@example.com",
            MembershipId = "SEN-001", Role = "SeniorRep", Status = "ACTIVE", SectionId = homeSection.Id,
            PasswordHash = "not-used", SignatureData = "data:image/png;base64,AA=="
        };
        var designation = new Designation
        {
            Id = Guid.NewGuid(), Code = DesignationCodes.StewardshipConvener, Name = "Stewardship Convener",
            RequiredRole = "SeniorRep", FunctionalJurisdiction = "STEWARDSHIP", IsActive = true
            ,CapabilitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { WorkflowCapabilities.FrbVerifyStewardship, WorkflowCapabilities.FrbOrganizationScope })
        };
        var area = new FunctionalArea { Id = Guid.NewGuid(), Code = "STEWARDSHIP", Name = "Stewardship", IsActive = true, IsSystem = true };
        designation.FunctionalAreaId = area.Id;
        var frb = new FinancialRecordBook
        {
            Id = Guid.NewGuid(), FrbNumber = "FRB-CROSS-SECTION", PeriodStart = new DateOnly(2026, 9, 28),
            PeriodEnd = new DateOnly(2026, 10, 4), SectionId = sourceSection.Id,
            Status = FinancialRecordBookStatuses.AwaitingVerification, CreatedById = Guid.NewGuid()
        };
        db.AddRange(homeSection, sourceSection, actor, designation, area, frb);
        db.OfficeAppointments.Add(new OfficeAppointment
        {
            Id = Guid.NewGuid(), UserId = actor.Id, DesignationId = designation.Id, FunctionalAreaId = area.Id,
            ScopeType = AuthorityScopeTypes.Congregation, EffectiveFromUtc = DateTime.UtcNow.AddDays(-1), IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new FinancialRecordBookService(db, new UnusedReferences(), new GlobalAccess(), new TestCurrentUser(actor.Id), new WorkflowAuthorizationService(db));
        await service.VerifyAsync(frb.Id, new FrbVerificationRequest(FrbOfficerTypes.StewardshipConvener, "Reviewed"), actor.Id, CancellationToken.None);

        var verification = await db.FrbVerifications.SingleAsync();
        Assert.Equal(actor.Id, verification.VerifiedById);
        Assert.Contains("Stewardship Convener", verification.OfficerRole);
    }

    [Fact]
    public async Task PreviewAsync_SupportsDirect_ServiceCollection_AndCombinedSourcesUsingLineAmounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var weekly = new ContributionType { Id = Guid.NewGuid(), Code = "WEEKLY", Name = "Weekly Offering" };
        var tithe = new ContributionType { Id = Guid.NewGuid(), Code = "TITHE", Name = "Tithe" };
        var source = new ContributionSource { Id = Guid.NewGuid(), Name = "Cash" };
        var assessment = new FrbAssessmentRule { Id = Guid.NewGuid(), Name = "Standard Assessment",
            Classification = "DEDUCTION", CalculationMethod = "PERCENTAGE", EffectiveFrom = new DateOnly(2026, 1, 1) };
        assessment.ContributionTypes.Add(new FrbAssessmentRuleContributionType { FrbAssessmentRuleId = assessment.Id, ContributionTypeId = weekly.Id });
        assessment.ContributionTypes.Add(new FrbAssessmentRuleContributionType { FrbAssessmentRuleId = assessment.Id, ContributionTypeId = tithe.Id });
        assessment.AllocationLines.Add(new FrbAssessmentAllocationLine { Id = Guid.NewGuid(), FrbAssessmentRuleId = assessment.Id,
            Beneficiary = "Synod", Rate = 10m, DisplayOrder = 1 });
        var direct = Contribution("CON-LINES", 999m, weekly.Id, source.Id);
        direct.Allocations.Add(new ContributionAllocation { Id = Guid.NewGuid(), ContributionId = direct.Id,
            ContributionTypeId = weekly.Id, AllocatedAmount = 40m, IsPrimary = true });
        direct.Allocations.Add(new ContributionAllocation { Id = Guid.NewGuid(), ContributionId = direct.Id,
            ContributionTypeId = tithe.Id, AllocatedAmount = 60m });
        var serviceType = new ServiceType { Id = Guid.NewGuid(), Code = "SUNDAY", Name = "Sunday Service" };
        var serviceEvent = new ServiceEvent { Id = Guid.NewGuid(), ServiceTypeId = serviceType.Id,
            StartsAtUtc = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc) };
        var collection = new ServiceCollection { Id = Guid.NewGuid(), CollectionNumber = "SC-001",
            ServiceEventId = serviceEvent.Id, ContributionSourceId = source.Id, Status = ServiceCollectionStatuses.Approved,
            PurposeTotal = 250m, TenderTotal = 250m };
        var entry = new ServiceCollectionEntry { Id = Guid.NewGuid(), ServiceCollectionId = collection.Id,
            PartyType = "ANONYMOUS", ContributorNameSnapshot = "Anonymous" };
        var serviceWeekly = Contribution("CON-SERVICE-WEEKLY", 150m, weekly.Id, source.Id);
        serviceWeekly.OriginType = "SERVICE_COLLECTION";
        serviceWeekly.Allocations.Add(new ContributionAllocation { Id = Guid.NewGuid(), ContributionId = serviceWeekly.Id,
            ContributionTypeId = weekly.Id, AllocatedAmount = 150m, PercentageAllocation = 100m, IsPrimary = true });
        var serviceTithe = Contribution("CON-SERVICE-TITHE", 100m, tithe.Id, source.Id);
        serviceTithe.OriginType = "SERVICE_COLLECTION";
        serviceTithe.Allocations.Add(new ContributionAllocation { Id = Guid.NewGuid(), ContributionId = serviceTithe.Id,
            ContributionTypeId = tithe.Id, AllocatedAmount = 100m, PercentageAllocation = 100m, IsPrimary = true });
        entry.Lines.Add(new ServiceCollectionEntryLine { Id = Guid.NewGuid(), ServiceCollectionEntryId = entry.Id,
            ContributionTypeId = weekly.Id, Amount = 150m, ContributionId = serviceWeekly.Id });
        entry.Lines.Add(new ServiceCollectionEntryLine { Id = Guid.NewGuid(), ServiceCollectionEntryId = entry.Id,
            ContributionTypeId = tithe.Id, Amount = 100m, ContributionId = serviceTithe.Id });
        collection.Entries.Add(entry);
        db.AddRange(weekly, tithe, source, assessment, direct, serviceWeekly, serviceTithe, serviceType, serviceEvent, collection);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = new FinancialRecordBookService(db, new UnusedReferences(), new GlobalAccess(),
            new TestCurrentUser(Guid.NewGuid()), new WorkflowAuthorizationService(db));
        var periodStart = new DateTime(2026, 9, 28); var periodEnd = new DateTime(2026, 10, 4);

        var directOnly = await service.PreviewAsync(new SaveFinancialRecordBookRequest(periodStart, periodEnd, null, null,
            [direct.Id], [], [assessment.Id]), CancellationToken.None);
        var collectionOnly = await service.PreviewAsync(new SaveFinancialRecordBookRequest(periodStart, periodEnd, null, null,
            [], [collection.Id], [assessment.Id]), CancellationToken.None);
        var combined = await service.PreviewAsync(new SaveFinancialRecordBookRequest(periodStart, periodEnd, null, null,
            [direct.Id], [collection.Id], [assessment.Id]), CancellationToken.None);

        Assert.Equal(100m, Gross(directOnly));
        Assert.Equal(250m, Gross(collectionOnly));
        Assert.Equal(350m, Gross(combined));
    }

    [Fact]
    public async Task EligibleContributionsAsync_ExcludesHistoricalServiceCollectionProjections()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var type = new ContributionType { Id = Guid.NewGuid(), Code = "WEEKLY", Name = "Weekly Offering" };
        var source = new ContributionSource { Id = Guid.NewGuid(), Name = "Cash" };
        var direct = Contribution("CON-DIRECT", 100m, type.Id, source.Id);
        var projected = Contribution("CON-PROJECTED", 100m, type.Id, source.Id); projected.OriginType = "SERVICE_COLLECTION";
        db.AddRange(type, source, direct, projected); await db.SaveChangesAsync();
        var service = new FinancialRecordBookService(db, new UnusedReferences(), new GlobalAccess(),
            new TestCurrentUser(Guid.NewGuid()), new WorkflowAuthorizationService(db));

        var result = await service.EligibleContributionsAsync(new DateTime(2026, 9, 28), new DateTime(2026, 10, 4),
            null, null, null, CancellationToken.None);
        var json = JsonSerializer.Serialize(result);

        Assert.Contains("CON-DIRECT", json);
        Assert.DoesNotContain("CON-PROJECTED", json);
    }

    private static decimal Gross(object result) => JsonDocument.Parse(JsonSerializer.Serialize(result)).RootElement
        .GetProperty("GrossContributions").GetDecimal();

    private sealed class TestCurrentUser(Guid userId) : ICurrentUserService
    {
        public Guid? GetUserId() => userId;
        public string? GetUserEmail() => "test@example.com";
        public string? GetUserRole() => "SuperAdmin";
        public bool IsAuthenticated() => true;
    }

    private static Contribution Contribution(string number, decimal amount, Guid typeId, Guid sourceId) => new()
    {
        Id = Guid.NewGuid(), ContributionNumber = number, ContributionTypeId = typeId, SourceId = sourceId,
        Amount = amount, ContributionDate = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc),
        PaymentMethod = "CASH", Status = "CONFIRMED"
    };

    private sealed class UnusedReferences : IReferenceNumberService
    {
        public Task<string> NextAsync(string referenceType, DateOnly? date = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class GlobalAccess : IGroupAccessService
    {
        public bool HasGlobalAccess() => true;
        public Task<List<Guid>> GetAccessibleGroupIdsAsync() => Task.FromResult(new List<Guid>());
        public Task<bool> CanAccessGroupAsync(Guid groupId) => Task.FromResult(true);
        public Task<List<Guid>> GetAccessibleSectionIdsAsync() => Task.FromResult(new List<Guid>());
        public Task<List<Guid>> GetAccessibleCommitteeIdsAsync() => Task.FromResult(new List<Guid>());
        public Task<bool> CanAccessEntityAsync(Guid? groupId, Guid? sectionId, Guid? committeeId) => Task.FromResult(true);
    }
}
