using eMilimo.API.Controllers;
using eMilimo.API.Services;
using eMilimo.Core.DTOs;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace eMilimo.API.Tests.Controllers;

public sealed class FrbSettingsControllerTests
{
    [Fact]
    public async Task UpdateRule_PreservesExistingAllocationRowAndAddsNewAllocation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging()
            .LogTo(Console.WriteLine, LogLevel.Information)
            .Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var actorId = Guid.NewGuid();
        var contributionTypeId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();
        db.ContributionTypes.Add(new ContributionType
        {
            Id = contributionTypeId,
            Code = "WEEKLY",
            Name = "Weekly Offering"
        });
        db.FrbAssessmentRules.Add(new FrbAssessmentRule
        {
            Id = ruleId,
            Name = "Weekly Offering Assessment",
            Classification = "DEDUCTION",
            CalculationMethod = "PERCENTAGE",
            EffectiveFrom = new DateOnly(2026, 10, 4),
            IsActive = true,
            ContributionTypes =
            [
                new FrbAssessmentRuleContributionType
                {
                    FrbAssessmentRuleId = ruleId,
                    ContributionTypeId = contributionTypeId
                }
            ],
            AllocationLines =
            [
                new FrbAssessmentAllocationLine
                {
                    Id = allocationId,
                    FrbAssessmentRuleId = ruleId,
                    Beneficiary = "Synod",
                    Rate = 10m,
                    DisplayOrder = 0
                }
            ]
        });
        await db.SaveChangesAsync();

        var controller = new FrbSettingsController(db, new TestCurrentUserService(actorId), new TestSystemLogWriter())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var request = new SaveFrbAssessmentRuleRequest(
            "Weekly Offering Assessment",
            string.Empty,
            "ALLOCATION",
            "PERCENTAGE",
            new DateOnly(2026, 10, 4),
            null,
            true,
            string.Empty,
            [contributionTypeId],
            [
                new SaveFrbAssessmentAllocationLineRequest("Synod", 60m, null, false, 0, string.Empty),
                new SaveFrbAssessmentAllocationLineRequest("Congregation", 40m, null, true, 1, string.Empty)
            ]);

        var result = await controller.UpdateRule(ruleId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        db.ChangeTracker.Clear();
        var savedRule = await db.FrbAssessmentRules
            .Include(rule => rule.AllocationLines)
            .Include(rule => rule.ContributionTypes)
            .SingleAsync(rule => rule.Id == ruleId);
        Assert.Equal("ALLOCATION", savedRule.Classification);
        Assert.Equal(2, savedRule.AllocationLines.Count);
        Assert.Contains(savedRule.AllocationLines, line => line.Id == allocationId && line.Rate == 60m);
        Assert.Contains(savedRule.AllocationLines, line => line.Beneficiary == "Congregation" && line.Rate == 40m);
        Assert.Single(savedRule.ContributionTypes);
    }

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid? GetUserId() => userId;
        public string? GetUserEmail() => null;
        public string? GetUserRole() => "Admin";
        public bool IsAuthenticated() => true;
    }

    private sealed class TestSystemLogWriter : ISystemLogWriter
    {
        public Task WriteAsync(SystemLog entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}