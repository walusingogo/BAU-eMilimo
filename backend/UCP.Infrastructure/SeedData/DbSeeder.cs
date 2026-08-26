using System.Security.Cryptography;
using BCryptHash = BCrypt.Net.BCrypt;
using Microsoft.EntityFrameworkCore;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;

namespace UCP.Infrastructure.SeedData;

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
        await db.SaveChangesAsync();
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
            ["minister_approval_threshold"] = ("50000", "Amount requiring Minister approval"),
            ["default_fiscal_year"] = (DateTime.UtcNow.Year.ToString(), "Default fiscal year")
        };
        var existing = await db.SystemConfigs.Select(x => x.ConfigKey).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var item in defaults.Where(x => !existing.Contains(x.Key, StringComparer.OrdinalIgnoreCase)))
            db.SystemConfigs.Add(new SystemConfig { Id = Guid.NewGuid(), ConfigKey = item.Key, ConfigValue = item.Value.Value,
                Description = item.Value.Description, CreatedDate = now, UpdatedDate = now, Version = 1 });
    }
}
