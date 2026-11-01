using System.Text.Json;
using eMilimo.API.Controllers;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eMilimo.API.Tests.Controllers;

public sealed class AuthorizationMaintenanceControllerTests
{
    [Fact]
    public async Task DesignationCreate_UsesStableRoleFunctionalAreaAndRequestedStatus()
    {
        await using var db = CreateDb();
        var area = Area("WCF");
        db.FunctionalAreas.Add(area);
        await db.SaveChangesAsync();
        var controller = new DesignationsController(db, new TestCurrentUser(), new TestLogWriter());

        var result = await controller.Create(new DesignationsController.DesignationRequest(
            "WCF_CONVENER", "WCF Convener", "SeniorRep", null, null,
            [WorkflowCapabilities.RequisitionEntityReview], false, area.Id));

        Assert.IsType<CreatedAtActionResult>(result);
        var saved = await db.Designations.SingleAsync();
        Assert.Equal("SeniorRep", saved.RequiredRole);
        Assert.Equal(area.Id, saved.FunctionalAreaId);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task DesignationCreate_RejectsOfficeNameAsAnAuthorityRole()
    {
        await using var db = CreateDb();
        var area = Area("TREASURY");
        db.FunctionalAreas.Add(area);
        await db.SaveChangesAsync();
        var controller = new DesignationsController(db, new TestCurrentUser(), new TestLogWriter());

        var result = await controller.Create(new DesignationsController.DesignationRequest(
            "CUSTOM_TREASURER", "Custom Treasurer", "Treasurer", null, null, [], true, area.Id));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Designations);
    }

    [Fact]
    public async Task DesignationUpdate_RejectsUndefinedAuthorityRole()
    {
        await using var db = CreateDb();
        var area = Area("TREASURY");
        var designation = new Designation
        {
            Id = Guid.NewGuid(), Code = "TREASURER", Name = "Treasurer", RequiredRole = "Treasurer",
            FunctionalJurisdiction = area.Code, FunctionalAreaId = area.Id, IsSystem = true, IsActive = true
        };
        db.AddRange(area, designation);
        await db.SaveChangesAsync();
        var controller = new DesignationsController(db, new TestCurrentUser(), new TestLogWriter());

        var result = await controller.Update(designation.Id, new DesignationsController.DesignationRequest(
            designation.Code, "Church Treasurer", designation.RequiredRole, null, "Invalid authority role",
            [WorkflowCapabilities.FrbFinalize], true, area.Id));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Treasurer", designation.Name);
    }

    [Fact]
    public async Task RolePermissionMatrix_ContainsOnlyTheSixAuthorityRoles()
    {
        await using var db = CreateDb();
        var controller = new RolePermissionsController(db, new TestRolePermissions(), new TestCurrentUser(), new TestLogWriter());

        var result = Assert.IsType<OkObjectResult>(await controller.GetMatrix());
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var roles = document.RootElement.GetProperty("roles").EnumerateArray()
            .Select(x => x.GetProperty("role").GetString()).ToArray();

        Assert.Equal(new[] { "SuperAdmin", "Admin", "Auditor", "ChurchAuth", "SeniorRep", "GroupRep" }, roles);
        Assert.DoesNotContain("Treasurer", roles);
        Assert.DoesNotContain("Secretary", roles);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static FunctionalArea Area(string code) => new()
    {
        Id = Guid.NewGuid(), Code = code, Name = code, IsActive = true, IsSystem = true
    };

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public string? GetUserEmail() => "admin@example.com";
        public string? GetUserRole() => "Admin";
        public bool IsAuthenticated() => true;
    }

    private sealed class TestLogWriter : ISystemLogWriter
    {
        public Task WriteAsync(SystemLog entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestRolePermissions : IRolePermissionService
    {
        public Task<bool> HasAsync(string? role, string permission) => Task.FromResult(true);
        public Task<string[]> GetAsync(string? role) => Task.FromResult(Array.Empty<string>());
        public Task InvalidateAsync(string role) => Task.CompletedTask;
    }
}
