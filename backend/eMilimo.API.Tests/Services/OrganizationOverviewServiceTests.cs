using eMilimo.API.Payments;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class OrganizationOverviewServiceTests
{
    [Fact]
    public async Task Dashboard_OrdersContributionBreakdownBeforeProjection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var overview = new OrganizationOverview
        {
            Id = Guid.NewGuid(), OrganizationName = "Test Congregation", IsStale = false,
            TotalContributions = 30m, LastUpdatedUtc = DateTime.UtcNow
        };
        var tithe = new ContributionType { Id = Guid.NewGuid(), Code = "TITHE", Name = "Tithe" };
        var offering = new ContributionType { Id = Guid.NewGuid(), Code = "OFFERING", Name = "Offering" };
        db.AddRange(overview, tithe, offering);
        db.OrganizationContributionTypeSummaries.AddRange(
            new OrganizationContributionTypeSummary { Id = Guid.NewGuid(), OrganizationOverviewId = overview.Id, ContributionTypeId = tithe.Id, TotalAmount = 20m, ContributionCount = 1 },
            new OrganizationContributionTypeSummary { Id = Guid.NewGuid(), OrganizationOverviewId = overview.Id, ContributionTypeId = offering.Id, TotalAmount = 10m, ContributionCount = 1 });
        await db.SaveChangesAsync();

        var result = await new OrganizationOverviewService(db).GetOrganizationOverviewAsync();

        Assert.Equal(["OFFERING", "TITHE"], result.ContributionTypeBreakdowns.Select(x => x.Code));
    }
}
