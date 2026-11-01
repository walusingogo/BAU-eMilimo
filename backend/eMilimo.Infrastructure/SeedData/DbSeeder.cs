using System.Security.Cryptography;
using BCryptHash = BCrypt.Net.BCrypt;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Infrastructure.Data;
using System.Text.Json;

namespace eMilimo.Infrastructure.SeedData;

/// <summary>
/// Creates only the minimum records required to reach the first-run setup.
/// Business/reference data belongs to setup, import or normal administration.
/// Existing installations are never overwritten.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedBootstrapSuperAdminAsync(db);
        await SeedEssentialConfigAsync(db);
        await SeedReferenceNumberingAsync(db);
        await SeedFunctionalAreasAsync(db);
        await SeedMembershipAndCollectionDefaultsAsync(db);
        await db.SaveChangesAsync();
        await SeedDesignationsAsync(db);
        await SeedOrganizationDefaultsAsync(db);
        await db.SaveChangesAsync();
    }

    private static async Task SeedMembershipAndCollectionDefaultsAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var serviceTypes = new (string Code, string Name, int Order)[]
        {
            ("SUNDAY_SERVICE", "Sunday Service", 10), ("MIDWEEK_SERVICE", "Midweek Service", 20),
            ("FUNERAL", "Funeral", 30), ("CONFERENCE", "Conference", 40),
            ("FUNDRAISING", "Fundraising", 50), ("SPECIAL_SERVICE", "Special Service", 60),
            ("OTHER", "Other", 90)
        };
        var existingTypes = await db.ServiceTypes.Select(x => x.Code).ToListAsync();
        foreach (var item in serviceTypes.Where(x => !existingTypes.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
            db.ServiceTypes.Add(new ServiceType { Id = Guid.NewGuid(), Code = item.Code, Name = item.Name,
                DisplayOrder = item.Order, IsActive = true, CreatedDate = now, UpdatedDate = now, Version = 1 });

        var paymentMethods = new (string Code, string Name, string Category, bool Reference, bool Bank, bool Attachment, bool Fcy, int Order)[]
        {
            ("CASH", "Cash", "CASH", false, false, false, true, 10),
            ("BANK_TRANSFER", "Bank Transfer", "BANK", true, true, false, true, 20),
            ("MOBILE_MONEY", "Mobile Money", "DIGITAL", true, false, false, true, 30),
            ("AIRTEL_MONEY", "Airtel Money", "DIGITAL", true, false, false, false, 31),
            ("MTN_MOMO", "MTN Mobile Money", "DIGITAL", true, false, false, false, 32),
            ("ZAMTEL_MONEY", "Zamtel Money", "DIGITAL", true, false, false, false, 33),
            ("POS_CARD", "POS / Card", "CARD", true, true, true, true, 40),
            ("CHEQUE", "Cheque", "CHEQUE", true, true, true, false, 50),
            ("OTHER", "Other", "OTHER", true, false, false, true, 90)
        };
        var existingMethods = await db.PaymentMethodDefinitions.Select(x => x.Code).ToListAsync();
        foreach (var item in paymentMethods.Where(x => !existingMethods.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
            db.PaymentMethodDefinitions.Add(new PaymentMethodDefinition { Id = Guid.NewGuid(), Code = item.Code, Name = item.Name,
                Category = item.Category, RequiresReference = item.Reference, RequiresBankAccount = item.Bank,
                RequiresAttachment = item.Attachment, SupportsForeignCurrency = item.Fcy, DisplayOrder = item.Order,
                IsActive = true, CreatedDate = now, UpdatedDate = now, Version = 1 });

        if (!await db.MembershipIntegrationConfigurations.AnyAsync(x => x.ProviderCode == "LOCAL"))
            db.MembershipIntegrationConfigurations.Add(new MembershipIntegrationConfiguration { Id = Guid.NewGuid(),
                ProviderCode = "LOCAL", IsEnabled = false, UseLocalFallback = true, TimeoutSeconds = 15,
                FieldOwnershipMode = "EXTERNAL_WITH_LOCAL_FALLBACK", CreatedDate = now, UpdatedDate = now, Version = 1 });
        if (!await db.MembershipIntegrationConfigurations.AnyAsync(x => x.ProviderCode == "REST_API"))
            db.MembershipIntegrationConfigurations.Add(new MembershipIntegrationConfiguration { Id = Guid.NewGuid(),
                ProviderCode = "REST_API", IsEnabled = false, UseLocalFallback = true, TimeoutSeconds = 15,
                FieldOwnershipMode = "EXTERNAL_WITH_LOCAL_FALLBACK", CreatedDate = now, UpdatedDate = now, Version = 1 });

        if (!await db.ContributionTypes.AnyAsync(x => x.Code == "ASSET_HIRE"))
            db.ContributionTypes.Add(new ContributionType { Id = Guid.NewGuid(), Code = "ASSET_HIRE", Name = "Asset Hire",
                Description = "Income received from hiring church assets, fleet or equipment.", IsActive = true, DisplayOrder = 90,
                CreatedDate = now, UpdatedDate = now, Version = 1 });
        if (!await db.ContributionSources.AnyAsync(x => x.Name == "Asset Hire"))
            db.ContributionSources.Add(new ContributionSource { Id = Guid.NewGuid(), Name = "Asset Hire",
                Description = "Receipts generated by approved paid asset checkouts.", IsActive = true,
                CreatedDate = now, UpdatedDate = now, Version = 1 });
    }

    private static async Task SeedOrganizationDefaultsAsync(AppDbContext db)
    {
        var groups = new (string Name, string[] Aliases)[] {
            ("Women's Fellowship / WCF", ["Women's Fellowship / WCF", "Women's Fellowship", "WCF"]),
            ("Men's Fellowship / MCF", ["Men's Fellowship / MCF", "Men's Fellowship", "MCF"]),
            ("Youth Ministry", ["Youth Ministry", "Youth"]),
            ("Sunday School / Children's Ministry", ["Sunday School / Children's Ministry", "Sunday School", "Children's Ministry"]),
            ("Boys Brigade", ["Boys Brigade"]), ("Girls Brigade", ["Girls Brigade"]), ("Choir", ["Choir"]),
            ("Prayer Ministry", ["Prayer Ministry"]), ("Outreach Ministry", ["Outreach Ministry"]),
            ("Ushering Ministry", ["Ushering Ministry", "Ushers"])
        };
        var existingGroups = (await db.Groups.Where(x => x.GroupType == "ChurchGroup").Select(x => x.GroupName).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in groups.Where(x => !x.Aliases.Any(existingGroups.Contains)))
            db.Groups.Add(new Group { Id = Guid.NewGuid(), GroupName = item.Name, GroupType = "ChurchGroup",
                Description = "Configurable UCZ organisational default.", IsActive = true });

        var committees = new[] { "Stewardship Committee", "Finance Committee",
            "Doctrine, Worship and Evangelism Committee (DWE)", "Christian Training Committee",
            "Community Development and Social Justice Committee (CDSJC)", "Communication and Literature Committee",
            "Diaconal Committee", "Procurement Committee", "Projects Committee" };
        var existingCommittees = (await db.Committees.Select(x => x.CommitteeName).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in committees.Where(x => !existingCommittees.Contains(x)))
            db.Committees.Add(new Committee { Id = Guid.NewGuid(), CommitteeName = name,
                Description = "Configurable UCZ organisational default.", IsActive = true });
    }

    private static async Task SeedFunctionalAreasAsync(AppDbContext db)
    {
        var names = new Dictionary<string, string>
        {
            ["MINISTRY"] = "Ministry", ["SECRETARIAT"] = "Secretariat", ["ACCOUNTING"] = "Accounting",
            ["TREASURY"] = "Treasury", ["FINANCE"] = "Finance", ["STEWARDSHIP"] = "Stewardship",
            ["DWE"] = "Doctrine, Worship and Evangelism", ["WCF"] = "Women's Christian Fellowship",
            ["MCF"] = "Men's Christian Fellowship", ["YOUTH"] = "Youth Ministry",
            ["SUNDAY_SCHOOL"] = "Sunday School", ["CHRISTIAN_TRAINING"] = "Christian Training",
            ["MEMBERSHIP"] = "Membership", ["CDSJC"] = "Community Development and Social Justice",
            ["COMMUNICATIONS"] = "Communications and Literature", ["DIACONAL"] = "Diaconal",
            ["SECTION_ADMINISTRATION"] = "Section Administration", ["CHURCH_OVERSIGHT"] = "Church Oversight",
            ["AUDIT"] = "Audit", ["OTHER"] = "Other"
        };
        var existing = await db.FunctionalAreas.Select(x => x.Code).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var code in FunctionalAreaCodes.System.Where(x => !existing.Contains(x, StringComparer.OrdinalIgnoreCase)))
            db.FunctionalAreas.Add(new FunctionalArea { Id = Guid.NewGuid(), Code = code, Name = names[code],
                IsSystem = true, IsActive = true, CreatedDate = now, UpdatedDate = now, Version = 1 });
    }

    private static async Task SeedDesignationsAsync(AppDbContext db)
    {
        var defaults = new[]
        {
            (DesignationCodes.StewardshipConvener, "Stewardship Convener", nameof(UserRole.SeniorRep), "STEWARDSHIP", "Reviews stewardship items from every committee, section and group.", new[] { WorkflowCapabilities.FrbVerifyStewardship, WorkflowCapabilities.FrbOrganizationScope, WorkflowCapabilities.ServiceCollectionReview, WorkflowCapabilities.ServiceCollectionVarianceApprove }),
            (DesignationCodes.Elder, "Elder", nameof(UserRole.SeniorRep), "CHURCH_OVERSIGHT", "Performs elder oversight duties across the organization.", new[] { WorkflowCapabilities.RequisitionEntityReview }),
            (DesignationCodes.Steward, "Steward", nameof(UserRole.GroupRep), "STEWARDSHIP", "Performs stewardship duties for assigned organizational membership.", new[] { WorkflowCapabilities.RequisitionEntityReview, WorkflowCapabilities.ServiceCollectionManage, WorkflowCapabilities.ServiceCollectionReview, WorkflowCapabilities.ServiceCollectionVarianceApprove }),
            (DesignationCodes.SundaySchoolTeacher, "Sunday School Teacher", nameof(UserRole.GroupRep), "SUNDAY_SCHOOL", "Performs Sunday School duties for assigned organizational membership.", new[] { WorkflowCapabilities.RequisitionEntityReview }),
            (DesignationCodes.Minister, "Minister", nameof(UserRole.ChurchAuth), "MINISTRY", "Performs organization-wide ministerial duties.", new[] { WorkflowCapabilities.RequisitionMinisterApproval, WorkflowCapabilities.FrbVerifyMinister, WorkflowCapabilities.FrbOrganizationScope }),
            (DesignationCodes.Treasurer, "Treasurer", nameof(UserRole.ChurchAuth), "TREASURY", "Performs organization-wide treasury duties.", new[] { WorkflowCapabilities.RequisitionTreasurerApproval, WorkflowCapabilities.ContributionReview, WorkflowCapabilities.FrbManage, WorkflowCapabilities.FrbVerifyTreasurer, WorkflowCapabilities.FrbFinalize, WorkflowCapabilities.FrbOrganizationScope, WorkflowCapabilities.ServiceCollectionReview, WorkflowCapabilities.ServiceCollectionVarianceApprove }),
            (DesignationCodes.Accountant, "Accountant", nameof(UserRole.ChurchAuth), "ACCOUNTING", "Performs organization-wide accounting duties.", new[] { WorkflowCapabilities.RequisitionAccountantApproval, WorkflowCapabilities.ContributionReview, WorkflowCapabilities.FrbManage })
            ,(DesignationCodes.Convener, "Convener", nameof(UserRole.SeniorRep), "OTHER", "Reusable convener office; functional area and authority scope are selected on the appointment.", new[] { WorkflowCapabilities.FrbVerifyStewardship, WorkflowCapabilities.FrbOrganizationScope })
            ,(DesignationCodes.ViceConvener, "Vice Convener", nameof(UserRole.SeniorRep), "OTHER", "Reusable vice-convener office.", Array.Empty<string>())
            ,(DesignationCodes.ChurchSecretary, "Church Secretary", nameof(UserRole.ChurchAuth), "SECRETARIAT", "Congregation-wide church secretariat office.", new[] { WorkflowCapabilities.RequisitionChurchAuthority })
            ,(DesignationCodes.Chairperson, "Chairperson", nameof(UserRole.SeniorRep), "OTHER", "Reusable chairperson office.", Array.Empty<string>())
            ,(DesignationCodes.ViceChairperson, "Vice Chairperson", nameof(UserRole.SeniorRep), "OTHER", "Reusable vice-chairperson office.", Array.Empty<string>())
            ,(DesignationCodes.Secretary, "Secretary", nameof(UserRole.SeniorRep), "OTHER", "Reusable entity secretary office.", Array.Empty<string>())
            ,(DesignationCodes.ViceSecretary, "Vice Secretary", nameof(UserRole.SeniorRep), "OTHER", "Reusable vice-secretary office.", Array.Empty<string>())
            ,(DesignationCodes.Teacher, "Teacher", nameof(UserRole.GroupRep), "OTHER", "Reusable teaching office.", new[] { WorkflowCapabilities.RequisitionEntityReview })
            ,(DesignationCodes.Representative, "Representative", nameof(UserRole.SeniorRep), "OTHER", "Reusable representative office.", new[] { WorkflowCapabilities.RequisitionEntityReview })
            ,(DesignationCodes.CommitteeMember, "Committee Member", nameof(UserRole.GroupRep), "OTHER", "Reusable committee member office.", Array.Empty<string>())
            ,(DesignationCodes.Leader, "Leader", nameof(UserRole.GroupRep), "OTHER", "Reusable entity leader office.", Array.Empty<string>())
            ,(DesignationCodes.Member, "Member", nameof(UserRole.GroupRep), "OTHER", "Reusable entity member office.", Array.Empty<string>())
            ,(DesignationCodes.Other, "Other", nameof(UserRole.GroupRep), "OTHER", "Custom office title represented by a maintained designation.", Array.Empty<string>())
        };
        var existing = await db.Designations.Select(x => x.Code).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var item in defaults.Where(x => !existing.Contains(x.Item1, StringComparer.OrdinalIgnoreCase)))
        {
            var area = await db.FunctionalAreas.FirstAsync(x => x.Code == item.Item4);
            db.Designations.Add(new Designation
            {
                Id = Guid.NewGuid(), Code = item.Item1, Name = item.Item2, RequiredRole = item.Item3,
                FunctionalJurisdiction = item.Item4, FunctionalAreaId = area.Id, Description = item.Item5, IsSystem = true,
                CapabilitiesJson = JsonSerializer.Serialize(item.Item6),
                IsActive = true, CreatedDate = now, UpdatedDate = now, Version = 1
            });
        }

        // Capabilities evolve independently from the maintained designation record. Keep
        // system designations current without replacing administrator-authored metadata.
        var requiredCapabilities = defaults.ToDictionary(x => x.Item1, x => x.Item6, StringComparer.OrdinalIgnoreCase);
        var maintained = await db.Designations.Where(x => x.IsSystem).ToListAsync();
        foreach (var designation in maintained)
        {
            if (!requiredCapabilities.TryGetValue(designation.Code, out var required) || required.Length == 0) continue;
            var capabilities = string.IsNullOrWhiteSpace(designation.CapabilitiesJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(designation.CapabilitiesJson) ?? new List<string>();
            var changed = false;
            foreach (var capability in required.Where(x => !capabilities.Contains(x, StringComparer.OrdinalIgnoreCase)))
            {
                capabilities.Add(capability);
                changed = true;
            }
            if (!changed) continue;
            designation.CapabilitiesJson = JsonSerializer.Serialize(capabilities);
            designation.UpdatedDate = now;
            designation.Version++;
        }
    }

    private static async Task SeedBootstrapSuperAdminAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Title = "", FirstName = "System", LastName = "Owner", Gender = "",
            Email = "setup@localhost.invalid", PhoneNumber = "", MembershipId = "SUPER-0001",
            PasswordHash = BCryptHash.HashPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(48))),
            Role = UserRole.SuperAdmin.ToString(), Status = UserStatus.INACTIVE.ToString(), Locked = true,
            PasswordExpired = true, SignatureData = "", CreatedDate = now, UpdatedDate = now,
            LastProcessedTimeStamp = now, Version = 1
        });
    }

    private static async Task SeedEssentialConfigAsync(AppDbContext db)
    {
        var defaults = new Dictionary<string, (string Value, string Description)>(StringComparer.OrdinalIgnoreCase)
        {
            ["setup_completed"] = ("false", "Whether the Version 1 first-run setup has completed"),
            ["system_version"] = ("1.0.0", "Installed eMilimo product version"),
            ["church_name"] = ("Church Administration", "Organisation name displayed throughout eMilimo"),
            ["currency_code"] = ("ZMW", "ISO currency code"),
            ["frontend_base_url"] = ("http://localhost:4200", "Public frontend URL used in links"),
            ["giving_base_url"] = ("http://localhost:9200", "Public Digital Giving URL used in links and receipts"),
            ["minister_approval_threshold"] = ("50000", "Amount requiring Minister approval"),
            ["default_fiscal_year"] = (DateTime.UtcNow.Year.ToString(), "Default fiscal year")
        };
        var existing = await db.SystemConfigs.Select(x => x.ConfigKey).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var item in defaults.Where(x => !existing.Contains(x.Key, StringComparer.OrdinalIgnoreCase)))
            db.SystemConfigs.Add(new SystemConfig { Id = Guid.NewGuid(), ConfigKey = item.Key, ConfigValue = item.Value.Value,
                Description = item.Value.Description, CreatedDate = now, UpdatedDate = now, Version = 1 });
    }

    private static async Task SeedReferenceNumberingAsync(AppDbContext db)
    {
        var defaults = new Dictionary<string, (string Prefix, string Description)>
        {
            ["CONTRIBUTION"] = ("CON", "Contribution reference"),
            ["FRB"] = ("FRB", "Financial Record Book reference"),
            ["REQUISITION"] = ("REQ", "Requisition reference"),
            ["ASSET"] = ("AST", "Asset reference"),
            ["SERVICE_COLLECTION"] = ("SCL", "Service Collection reference")
        };
        var existing = await db.ReferenceNumberConfigurations.Select(x => x.ReferenceType).ToListAsync();
        foreach (var item in defaults.Where(x => !existing.Contains(x.Key)))
            db.ReferenceNumberConfigurations.Add(new ReferenceNumberConfiguration
            {
                Id = Guid.NewGuid(), ReferenceType = item.Key, Prefix = item.Value.Prefix,
                Description = item.Value.Description, EffectiveFrom = new DateOnly(2000, 1, 1),
                IsActive = true, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
            });
    }
}
