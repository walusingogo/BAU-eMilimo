using Microsoft.EntityFrameworkCore;
using BCryptHash = BCrypt.Net.BCrypt;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;

namespace UCP.Infrastructure.SeedData
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // ============================================
            // CREATE SECTIONS (1-11) IF MISSING
            // ============================================
            var existingSections = await context.Sections.CountAsync();
            if (existingSections == 0)
            {
                var sections = new List<Section>();
                for (int i = 1; i <= 11; i++)
                {
                    sections.Add(new Section
                    {
                        Id = Guid.NewGuid(),
                        SectionNumber = i,
                        SectionName = $"Section {i}",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    });
                }
                await context.Sections.AddRangeAsync(sections);
                Console.WriteLine($"Added {sections.Count} sections.");
            }

            // ============================================
            // CREATE GROUPS (Sections + Church Groups) IF MISSING
            // ============================================
            var existingGroups = await context.Groups.CountAsync();
            if (existingGroups == 0)
            {
                var groups = new List<Group>();

                // 1. Groups for each Section
                for (int i = 1; i <= 11; i++)
                {
                    groups.Add(new Group
                    {
                        Id = Guid.NewGuid(),
                        GroupName = $"Section {i}",
                        Description = $"Group representing Section {i}",
                        GroupType = "Section",
                        IsActive = true,
                        LeaderId = null,
                        ParentGroupId = null,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    });
                }

                // 2. Church Groups
                var churchGroups = new[] { "Boys Brigade", "Sunday School", "Stewardship", "Girls Brigade", "CDSJ" };
                foreach (var name in churchGroups)
                {
                    groups.Add(new Group
                    {
                        Id = Guid.NewGuid(),
                        GroupName = name,
                        Description = $"Group: {name}",
                        GroupType = "ChurchGroup",
                        IsActive = true,
                        LeaderId = null,
                        ParentGroupId = null,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    });
                }

                await context.Groups.AddRangeAsync(groups);
                Console.WriteLine($"Added {groups.Count} groups.");
            }

            // ============================================
            // SEED FUND BALANCES FOR EACH GROUP (Fiscal Year 2026)
            // ============================================
            var grps = await context.Groups.ToListAsync();
            var fiscalYear = DateTime.UtcNow.Year; // e.g., 2026

            var fundBalances = new List<FundBalance>();
            foreach (var group in grps)
            {
                fundBalances.Add(new FundBalance
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    FiscalYear = fiscalYear,
                    BudgetedBalance = 50000m,   
                    OwnFundsBalance = 10000m,   
                    CommittedBudgetedBalance = 0m,   
                    CommittedOwnFundsBalance = 0m,    
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    LastProcessedTimeStamp = DateTime.UtcNow,
                    Version = 1
                });
            }

            if (fundBalances.Any())
            {
                await context.FundBalances.AddRangeAsync(fundBalances);
                Console.WriteLine($"Added {fundBalances.Count} fund balances.");
            }

            // ============================================
            // CREATE SUPER ADMIN IF MISSING
            // ============================================
            var hasSuperAdmin = await context.Users
                .AnyAsync(u => u.Role == UserRole.SuperAdmin.ToString());

            if (!hasSuperAdmin)
            {
                var superAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "Super",
                    MiddleName = null,
                    LastName = "Admin",
                    Gender = "Male",
                    Email = "superadmin",
                    PhoneNumber = "+260 97 1234567",
                    MembershipId = "SUPER-001",
                    PasswordHash = BCryptHash.HashPassword("superadmin"),
                    Role = UserRole.SuperAdmin.ToString(),
                    Status = UserStatus.ACTIVE.ToString(),
                    Locked = false,
                    PasswordAttempts = 0,
                    PasswordLockDate = null,
                    PasswordExpiryDate = DateTime.UtcNow.AddDays(90),
                    PasswordExpired = false,
                    SectionId = null,
                    GroupId = null,
                    SignatureData = string.Empty,
                    CreatedById = null,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedById = null,
                    UpdatedDate = DateTime.UtcNow,
                    LastProcessedTimeStamp = DateTime.UtcNow,
                    Version = 1
                };
                await context.Users.AddAsync(superAdmin);
                Console.WriteLine("Super Admin created.");
            }

            // ============================================
            // CREATE DEFAULT SYSTEM CONFIGS IF MISSING
            // ============================================
            var existingConfigs = await context.SystemConfigs.CountAsync();
            if (existingConfigs == 0)
            {
                var configs = new[]
                {
                    new SystemConfig
                    {
                        Id = Guid.NewGuid(),
                        ConfigKey = "minister_approval_threshold",
                        ConfigValue = "15000",
                        Description = "Amount in Kwacha above which Minister must approve",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    },
                    new SystemConfig
                    {
                        Id = Guid.NewGuid(),
                        ConfigKey = "max_section_number",
                        ConfigValue = "11",
                        Description = "Maximum number of church sections",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    }
                };
                await context.SystemConfigs.AddRangeAsync(configs);
                Console.WriteLine("System configs added.");
            }

            // ============================================
            // SAVE CHANGES
            // ============================================
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeding completed.");
            Console.WriteLine($"   - Sections: {await context.Sections.CountAsync()}");
            Console.WriteLine($"   - Groups: {await context.Groups.CountAsync()}");
            Console.WriteLine($"   - F: {await context.Groups.CountAsync()}");
            Console.WriteLine($"   - Users: {await context.Users.CountAsync()}");
            Console.WriteLine($"   - Configs: {await context.SystemConfigs.CountAsync()}");
        }
    }
}