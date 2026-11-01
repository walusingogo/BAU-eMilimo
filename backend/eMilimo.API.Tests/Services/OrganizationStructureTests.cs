using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class OrganizationStructureTests
{
    [Fact]
    public async Task Committees_SupportCongregationSectionAndGroupParents_ButNotBoth()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
        await using var db = new AppDbContext(options); await db.Database.EnsureCreatedAsync();
        var section = new Section { Id = Guid.NewGuid(), SectionNumber = 1, SectionName = "Section 1" };
        var group = new Group { Id = Guid.NewGuid(), GroupName = "WCF", GroupType = "ChurchGroup" };
        db.AddRange(section, group,
            new Committee { Id = Guid.NewGuid(), CommitteeName = "Congregation Committee" },
            new Committee { Id = Guid.NewGuid(), CommitteeName = "Section Committee", ParentSectionId = section.Id },
            new Committee { Id = Guid.NewGuid(), CommitteeName = "Group Committee", ParentGroupId = group.Id });
        await db.SaveChangesAsync();
        Assert.Equal(3, await db.Committees.CountAsync());

        db.Committees.Add(new Committee { Id = Guid.NewGuid(), CommitteeName = "Invalid Committee",
            ParentSectionId = section.Id, ParentGroupId = group.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
