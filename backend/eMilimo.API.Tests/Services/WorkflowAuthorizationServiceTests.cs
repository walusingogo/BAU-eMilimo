using System.Text.Json;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class WorkflowAuthorizationServiceTests
{
    [Fact]
    public async Task AccountantAppointment_GrantsReviewAndDisbursementToChurchAuthority()
    {
        await using var db = CreateDb();
        var user = ActiveUser("ChurchAuth");
        var designation = Designation("ACCOUNTANT", "ChurchAuth",
            WorkflowCapabilities.RequisitionAccountantApproval,
            WorkflowCapabilities.ContributionReview,
            WorkflowCapabilities.FrbManage);
        Assign(db, user, designation);
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.HasAsync(user.Id, WorkflowCapabilities.ContributionReview));
        Assert.Contains("requisitions.disburse", await service.GetPermissionKeysAsync(user.Id));
        Assert.Contains("frb.create", await service.GetPermissionKeysAsync(user.Id));
    }

    [Fact]
    public async Task StewardshipConvener_IsOrganizationWideButDoesNotGainEntityRequisitionReview()
    {
        await using var db = CreateDb();
        var user = ActiveUser("SeniorRep");
        var designation = Designation(DesignationCodes.StewardshipConvener, "SeniorRep",
            WorkflowCapabilities.FrbVerifyStewardship,
            WorkflowCapabilities.FrbOrganizationScope);
        Assign(db, user, designation);
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.HasAsync(user.Id, WorkflowCapabilities.FrbVerifyStewardship));
        Assert.True(await service.HasAsync(user.Id, WorkflowCapabilities.FrbOrganizationScope));
        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview));
    }

    [Fact]
    public async Task Appointment_GrantsReviewOnlyWhileAppointmentAndDesignationAreActive()
    {
        await using var db = CreateDb();
        var user = ActiveUser("SeniorRep");
        var designation = Designation(DesignationCodes.Elder, "SeniorRep", WorkflowCapabilities.RequisitionEntityReview);
        Assign(db, user, designation, effectiveTo: DateTime.UtcNow.AddMinutes(-1));
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview));

        db.OfficeAppointments.Single().EffectiveToUtc = null;
        designation.IsActive = false;
        await db.SaveChangesAsync();
        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview));
    }

    [Fact]
    public async Task ChurchAuthorityRole_GrantsNoWorkflowCapabilityWithoutAppointment()
    {
        await using var db = CreateDb();
        var user = ActiveUser("ChurchAuth");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.RequisitionChurchAuthority));
        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.RequisitionMinisterApproval));
        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.RequisitionAccountantApproval));
        Assert.False(await service.HasAsync(user.Id, WorkflowCapabilities.ContributionReview));
    }

    [Fact]
    public async Task GroupAppointment_GrantsCapabilityOnlyInsideItsExplicitScope()
    {
        await using var db = CreateDb();
        var user = ActiveUser("GroupRep"); var groupId = Guid.NewGuid(); var otherGroupId = Guid.NewGuid();
        var area = Area("SUNDAY_SCHOOL");
        var designation = Designation(DesignationCodes.Teacher, "GroupRep", WorkflowCapabilities.RequisitionEntityReview);
        Appoint(db, user, designation, area, AuthorityScopeTypes.Group, groupId: groupId);
        await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview, new AuthorizationResource(GroupId: groupId)));
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview, new AuthorizationResource(GroupId: otherGroupId)));
    }

    [Fact]
    public async Task FunctionalAreaAppointment_IsOrganizationWideOnlyForItsArea()
    {
        await using var db = CreateDb();
        var user = ActiveUser("SeniorRep"); var area = Area("STEWARDSHIP");
        var designation = Designation(DesignationCodes.Convener, "SeniorRep", WorkflowCapabilities.FrbVerifyStewardship);
        Appoint(db, user, designation, area, AuthorityScopeTypes.FunctionalArea);
        await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.FrbVerifyStewardship, new AuthorizationResource(FunctionalAreaCode: "STEWARDSHIP")));
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.FrbVerifyStewardship, new AuthorizationResource(FunctionalAreaCode: "WCF")));
    }

    [Fact]
    public async Task ExpiredOrInactiveAppointment_GrantsNoCapability()
    {
        await using var db = CreateDb();
        var user = ActiveUser("ChurchAuth"); var area = Area("TREASURY");
        var designation = Designation(DesignationCodes.Treasurer, "ChurchAuth", WorkflowCapabilities.FrbFinalize);
        var appointment = Appoint(db, user, designation, area, AuthorityScopeTypes.Congregation);
        appointment.EffectiveToUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.FrbFinalize, new AuthorizationResource()));

        appointment.EffectiveToUtc = null; area.IsActive = false; await db.SaveChangesAsync();
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.FrbFinalize, new AuthorizationResource()));
    }

    [Fact]
    public async Task MembershipAlone_DoesNotGrantAppointmentAuthority()
    {
        await using var db = CreateDb();
        var user = ActiveUser("GroupRep"); var group = new Group { Id = Guid.NewGuid(), GroupName = "Sunday School", GroupType = "ChurchGroup", IsActive = true };
        db.AddRange(user, group); db.UserGroupAssignments.Add(new UserGroupAssignment { UserId = user.Id, GroupId = group.Id }); await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview, new AuthorizationResource(GroupId: group.Id)));
    }

    [Fact]
    public async Task User_CanHoldMultipleSimultaneousAppointments()
    {
        await using var db = CreateDb(); var user = ActiveUser("ChurchAuth");
        var treasury = Area("TREASURY"); var ministry = Area("MINISTRY");
        Appoint(db, user, Designation(DesignationCodes.Treasurer, "ChurchAuth", WorkflowCapabilities.FrbFinalize), treasury, AuthorityScopeTypes.Congregation);
        Appoint(db, user, Designation(DesignationCodes.Minister, "ChurchAuth", WorkflowCapabilities.FrbVerifyMinister), ministry, AuthorityScopeTypes.Congregation);
        await db.SaveChangesAsync(); var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.FrbFinalize, new AuthorizationResource(FunctionalAreaCode: "TREASURY")));
        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.FrbVerifyMinister, new AuthorizationResource(FunctionalAreaCode: "MINISTRY")));
    }

    [Fact]
    public async Task ChurchSecretaryAppointment_GrantsCongregationWideChurchAuthority()
    {
        await using var db = CreateDb(); var user = ActiveUser("ChurchAuth"); var area = Area("SECRETARIAT");
        Appoint(db, user, Designation(DesignationCodes.ChurchSecretary, "ChurchAuth", WorkflowCapabilities.RequisitionChurchAuthority), area, AuthorityScopeTypes.Congregation);
        await db.SaveChangesAsync(); var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionChurchAuthority, new AuthorizationResource(GroupId: Guid.NewGuid())));
        Assert.True(await service.HasCongregationScopeAsync(user.Id));
    }

    [Fact]
    public async Task SuperAdminOverride_RemainsValidWithoutAppointment()
    {
        await using var db = CreateDb(); var user = ActiveUser("SuperAdmin"); db.Users.Add(user); await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);
        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.FrbFinalize, new AuthorizationResource(GroupId: Guid.NewGuid(), FunctionalAreaCode: "TREASURY")));
    }

    [Fact]
    public async Task FutureDatedAppointment_GrantsNoCapabilityUntilEffective()
    {
        await using var db = CreateDb();
        var user = ActiveUser("ChurchAuth");
        var appointment = Appoint(db, user,
            Designation(DesignationCodes.Treasurer, "ChurchAuth", WorkflowCapabilities.FrbFinalize),
            Area("TREASURY"), AuthorityScopeTypes.Congregation);
        appointment.EffectiveFromUtc = DateTime.UtcNow.AddDays(1);
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.FrbFinalize, new AuthorizationResource()));
    }

    [Fact]
    public async Task WcfConvener_HasOnlyTheConfiguredGroupScope()
    {
        await using var db = CreateDb();
        var user = ActiveUser("SeniorRep");
        var wcfId = Guid.NewGuid();
        Appoint(db, user, Designation(DesignationCodes.Convener, "SeniorRep", WorkflowCapabilities.RequisitionEntityReview),
            Area("WCF"), AuthorityScopeTypes.Group, groupId: wcfId);
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview,
            new AuthorizationResource(GroupId: wcfId, FunctionalAreaCode: "WCF")));
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview,
            new AuthorizationResource(GroupId: Guid.NewGuid(), FunctionalAreaCode: "WCF")));
    }

    [Fact]
    public async Task SundaySchoolTeacher_HasOnlyTheConfiguredGroupScope()
    {
        await using var db = CreateDb();
        var user = ActiveUser("GroupRep");
        var sundaySchoolId = Guid.NewGuid();
        Appoint(db, user, Designation(DesignationCodes.Teacher, "GroupRep", WorkflowCapabilities.RequisitionEntityReview),
            Area("SUNDAY_SCHOOL"), AuthorityScopeTypes.Group, groupId: sundaySchoolId);
        await db.SaveChangesAsync();

        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview,
            new AuthorizationResource(GroupId: sundaySchoolId, FunctionalAreaCode: "SUNDAY_SCHOOL")));
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.RequisitionEntityReview,
            new AuthorizationResource(GroupId: Guid.NewGuid(), FunctionalAreaCode: "SUNDAY_SCHOOL")));
    }

    [Fact]
    public async Task Steward_CanReviewSubmittedServiceCollectionOutsideAssignedEntityScope()
    {
        await using var db = CreateDb();
        var user = ActiveUser("GroupRep");
        Appoint(db, user,
            Designation(DesignationCodes.Steward, "GroupRep", WorkflowCapabilities.ServiceCollectionReview, WorkflowCapabilities.ServiceCollectionVarianceApprove),
            Area("STEWARDSHIP"), AuthorityScopeTypes.Section, sectionId: Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);
        var otherSection = new AuthorizationResource(SectionId: Guid.NewGuid(), FunctionalAreaCode: "STEWARDSHIP");

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.ServiceCollectionReview, otherSection));
        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.ServiceCollectionVarianceApprove, otherSection));
        Assert.Contains("service-collections.review", await service.GetPermissionKeysAsync(user.Id));
    }

    [Fact]
    public async Task NonStewardDesignation_DoesNotGainCongregationWideServiceCollectionReview()
    {
        await using var db = CreateDb();
        var user = ActiveUser("GroupRep");
        var assignedGroup = Guid.NewGuid();
        Appoint(db, user,
            Designation(DesignationCodes.Teacher, "GroupRep", WorkflowCapabilities.ServiceCollectionReview),
            Area("SUNDAY_SCHOOL"), AuthorityScopeTypes.Group, groupId: assignedGroup);
        await db.SaveChangesAsync();
        var service = new WorkflowAuthorizationService(db);

        Assert.True(await service.CanAsync(user.Id, WorkflowCapabilities.ServiceCollectionReview,
            new AuthorizationResource(GroupId: assignedGroup, FunctionalAreaCode: "SUNDAY_SCHOOL")));
        Assert.False(await service.CanAsync(user.Id, WorkflowCapabilities.ServiceCollectionReview,
            new AuthorizationResource(GroupId: Guid.NewGuid(), FunctionalAreaCode: "SUNDAY_SCHOOL")));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static User ActiveUser(string role) => new()
    {
        Id = Guid.NewGuid(), FirstName = "Test", LastName = "User", Email = $"{Guid.NewGuid():N}@example.com",
        MembershipId = Guid.NewGuid().ToString("N"), PasswordHash = "not-used", Role = role, Status = "ACTIVE"
    };

    private static Designation Designation(string code, string role, params string[] capabilities) => new()
    {
        Id = Guid.NewGuid(), Code = code, Name = code.Replace('_', ' '), RequiredRole = role,
        FunctionalJurisdiction = code, IsActive = true, CapabilitiesJson = JsonSerializer.Serialize(capabilities)
    };

    private static FunctionalArea Area(string code) => new() { Id = Guid.NewGuid(), Code = code, Name = code, IsActive = true, IsSystem = true };

    private static OfficeAppointment Appoint(AppDbContext db, User user, Designation designation, FunctionalArea area,
        string scope, Guid? sectionId = null, Guid? groupId = null, Guid? committeeId = null)
    {
        designation.FunctionalAreaId = area.Id;
        db.AddRange(user, designation, area);
        var result = new OfficeAppointment { Id = Guid.NewGuid(), UserId = user.Id, DesignationId = designation.Id,
            FunctionalAreaId = area.Id, ScopeType = scope, SectionId = sectionId, GroupId = groupId,
            CommitteeId = committeeId, EffectiveFromUtc = DateTime.UtcNow.AddDays(-1), IsActive = true };
        db.OfficeAppointments.Add(result); return result;
    }

    private static void Assign(AppDbContext db, User user, Designation designation, DateTime? effectiveTo = null)
    {
        var area = Area(designation.FunctionalJurisdiction);
        var appointment = Appoint(db, user, designation, area, AuthorityScopeTypes.Congregation);
        appointment.EffectiveToUtc = effectiveTo;
    }
}
