// backend/UCP.Infrastructure/SeedData/DbSeeder.cs
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
            // CREATE CHURCH GROUPS IF MISSING
            // ============================================
            var existingGroups = await context.Groups.CountAsync();
            if (existingGroups == 0)
            {
                var groups = new List<Group>();

                // Church Groups
                var churchGroups = new[] { 
                    "Boys Brigade", 
                    "Sunday School", 
                    "Stewardship", 
                    "Girls Brigade", 
                    "CDSJ",
                    "Women's Fellowship",
                    "Men's Fellowship",
                    "Youth Ministry",
                    "Choir",
                    "Ushering Ministry",
                    "Prayer Ministry",
                    "Outreach Ministry",
                    "Children's Ministry"
                };
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
            // CREATE COMMITTEES IF MISSING
            // ============================================
            if (!await context.Committees.AnyAsync())
            {
                var committeeNames = new[] { "Finance Committee", "Projects Committee", "Procurement Committee" };
                await context.Committees.AddRangeAsync(committeeNames.Select(name => new Committee
                {
                    Id = Guid.NewGuid(),
                    CommitteeName = name,
                    Description = $"Committee: {name}",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    LastProcessedTimeStamp = DateTime.UtcNow,
                    Version = 1
                }));
            }

            // ============================================
            // SEED FUND BALANCES FOR EACH GROUP (Fiscal Year 2026)
            // - FIXED: Checks for existing balances to avoid duplicate key violation
            // ============================================
            var grps = await context.Groups.Where(g => g.GroupType == "ChurchGroup").ToListAsync();
            var fiscalYear = DateTime.UtcNow.Year;

            // Get existing fund balances for this fiscal year to avoid duplicates
            var existingFundBalances = await context.FundBalances
                .Where(fb => fb.FiscalYear == fiscalYear)
                .Select(fb => fb.GroupId)
                .ToListAsync();

            var fundBalances = new List<FundBalance>();
            foreach (var group in grps)
            {
                // Skip if this group already has a fund balance for this fiscal year
                if (existingFundBalances.Contains(group.Id))
                    continue;

                var random = new Random();
                var budgetedBalance = random.Next(10000, 100000);
                var ownFundsBalance = random.Next(5000, 50000);

                fundBalances.Add(new FundBalance
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    FiscalYear = fiscalYear,
                    BudgetedBalance = budgetedBalance,
                    OwnFundsBalance = ownFundsBalance,
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
                Console.WriteLine($"Added {fundBalances.Count} new fund balances.");
            }
            else
            {
                Console.WriteLine($"No new fund balances to add. {grps.Count} groups already have balances for fiscal year {fiscalYear}.");
            }

            // ============================================
            // CREATE SYSTEM USERS IF MISSING
            // ============================================
            var existingUsers = await context.Users.CountAsync();
            if (existingUsers == 0)
            {
                var users = new List<User>();
                var defaultPassword = "password123"; // Same password for all non-superadmin users

                // 1. Super Admin (different password)
                users.Add(new User
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
                });

                // 2. Admin
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "Admin",
                    MiddleName = null,
                    LastName = "User",
                    Gender = "Male",
                    Email = "admin",
                    PhoneNumber = "+260 97 1234568",
                    MembershipId = "ADMIN-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.Admin.ToString(),
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
                });

                // 3. Accountant
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Ms.",
                    FirstName = "Jane",
                    MiddleName = "M.",
                    LastName = "Banda",
                    Gender = "Female",
                    Email = "accountant",
                    PhoneNumber = "+260 97 1234569",
                    MembershipId = "ACCT-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.Accountant.ToString(),
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
                });

                // 4. Minister
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Rev.",
                    FirstName = "John",
                    MiddleName = "A.",
                    LastName = "Phiri",
                    Gender = "Male",
                    Email = "minister",
                    PhoneNumber = "+260 97 1234570",
                    MembershipId = "MIN-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.Minister.ToString(),
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
                });

                // 5. Church Auth 1
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "John",
                    MiddleName = "A.",
                    LastName = "Tembo",
                    Gender = "Male",
                    Email = "churchauth1",
                    PhoneNumber = "+260 97 1234571",
                    MembershipId = "CAUTH-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.ChurchAuth.ToString(),
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
                });

                // 6. Church Auth 2
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mrs.",
                    FirstName = "Mary",
                    MiddleName = "Jane",
                    LastName = "Lungu",
                    Gender = "Female",
                    Email = "churchauth2",
                    PhoneNumber = "+260 97 1234572",
                    MembershipId = "CAUTH-002",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.ChurchAuth.ToString(),
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
                });

                // 7. Church Auth 3
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "David",
                    MiddleName = "J.",
                    LastName = "Banda",
                    Gender = "Male",
                    Email = "churchauth3",
                    PhoneNumber = "+260 97 1234573",
                    MembershipId = "CAUTH-003",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.ChurchAuth.ToString(),
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
                });

                // 8. Senior Rep 1
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "Adam",
                    MiddleName = "B.",
                    LastName = "Chanda",
                    Gender = "Male",
                    Email = "seniorrep1",
                    PhoneNumber = "+260 97 1234574",
                    MembershipId = "SREP-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.SeniorRep.ToString(),
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
                });

                // 9. Senior Rep 2
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Ms.",
                    FirstName = "Sarah",
                    MiddleName = "Clara",
                    LastName = "Zulu",
                    Gender = "Female",
                    Email = "seniorrep2",
                    PhoneNumber = "+260 97 1234575",
                    MembershipId = "SREP-002",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.SeniorRep.ToString(),
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
                });

                // 10. Group Rep
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "Chanda",
                    MiddleName = "Mumba",
                    LastName = "Kunda",
                    Gender = "Male",
                    Email = "grouprep",
                    PhoneNumber = "+260 97 1234576",
                    MembershipId = "GREP-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.GroupRep.ToString(),
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
                });

                // 11. Treasurer
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mrs.",
                    FirstName = "Maria",
                    MiddleName = "A.",
                    LastName = "Daka",
                    Gender = "Female",
                    Email = "treasurer",
                    PhoneNumber = "+260 97 1234577",
                    MembershipId = "TRS-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.Treasurer.ToString(),
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
                });

                // 12. Secretary
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Title = "Mr.",
                    FirstName = "Micheal",
                    MiddleName = null,
                    LastName = "Kunda",
                    Gender = "Male",
                    Email = "secretary",
                    PhoneNumber = "+260 97 1234578",
                    MembershipId = "SEC-001",
                    PasswordHash = BCryptHash.HashPassword(defaultPassword),
                    Role = UserRole.Secretary.ToString(),
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
                });

                await context.Users.AddRangeAsync(users);
                Console.WriteLine($"Added {users.Count} system users.");
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
                    },
                    new SystemConfig
                    {
                        Id = Guid.NewGuid(),
                        ConfigKey = "default_fiscal_year",
                        ConfigValue = DateTime.UtcNow.Year.ToString(),
                        Description = "Current fiscal year",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    },
                    new SystemConfig
                    {
                        Id = Guid.NewGuid(),
                        ConfigKey = "church_name",
                        ConfigValue = "United Church of Zambia",
                        Description = "Official church name",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    },
                    new SystemConfig
                    {
                        Id = Guid.NewGuid(),
                        ConfigKey = "currency_code",
                        ConfigValue = "ZMW",
                        Description = "Currency code used in the system",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        LastProcessedTimeStamp = DateTime.UtcNow,
                        Version = 1
                    }
                };
                await context.SystemConfigs.AddRangeAsync(configs);
                Console.WriteLine($"Added {configs.Length} system configs.");
            }

            if (!await context.SystemConfigs.AnyAsync(x => x.ConfigKey == "frontend_base_url"))
            {
                context.SystemConfigs.Add(new SystemConfig
                {
                    Id = Guid.NewGuid(),
                    ConfigKey = "frontend_base_url",
                    ConfigValue = "http://localhost:4200",
                    Description = "Public frontend base URL used for password reset links and other email links",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    LastProcessedTimeStamp = DateTime.UtcNow,
                    Version = 1
                });
                await context.SaveChangesAsync();
            }

            // ============================================
            // CREATE CONTRIBUTION TYPES IF MISSING
            // ============================================
            var existingTypes = await context.ContributionTypes.CountAsync();
            if (existingTypes == 0)
            {
                var types = new[]
                {
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "TITHE",
                        Name = "Tithe",
                        Description = "Regular tithe contributions",
                        IsActive = true,
                        DisplayOrder = 1,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "OFFERING",
                        Name = "Offering",
                        Description = "Church service offerings",
                        IsActive = true,
                        DisplayOrder = 2,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "SPECIAL",
                        Name = "Special Offering",
                        Description = "Special occasion offerings",
                        IsActive = true,
                        DisplayOrder = 3,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "BUILDING",
                        Name = "Building Fund",
                        Description = "Building fund contributions",
                        IsActive = true,
                        DisplayOrder = 4,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "MISSION",
                        Name = "Mission Fund",
                        Description = "Mission support contributions",
                        IsActive = true,
                        DisplayOrder = 5,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "BENEVOLENT",
                        Name = "Benevolent Fund",
                        Description = "Benevolent/charity contributions",
                        IsActive = true,
                        DisplayOrder = 6,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionType
                    {
                        Id = Guid.NewGuid(),
                        Code = "THANKSGIVING",
                        Name = "Thanksgiving Offering",
                        Description = "Thanksgiving and gratitude offerings",
                        IsActive = true,
                        DisplayOrder = 7,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    }
                };
                await context.ContributionTypes.AddRangeAsync(types);
                Console.WriteLine($"Added {types.Length} contribution types.");
            }

            // ============================================
            // CREATE CONTRIBUTION SOURCES IF MISSING
            // ============================================
            var existingSources = await context.ContributionSources.CountAsync();
            if (existingSources == 0)
            {
                var sources = new[]
                {
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Sunday Service",
                        Description = "Regular Sunday service collections",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Midweek Service",
                        Description = "Wednesday/midweek service collections",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Bank Transfer",
                        Description = "Direct bank transfers",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Mobile Money",
                        Description = "Mobile money payments (Airtel, MTN, etc.)",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Special Event",
                        Description = "Special church events and programs",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Online Payment",
                        Description = "Online payment platforms",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Pledge",
                        Description = "Member pledge contributions",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new ContributionSource
                    {
                        Id = Guid.NewGuid(),
                        Name = "Corporate Donation",
                        Description = "Donations from corporate entities",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    }
                };
                await context.ContributionSources.AddRangeAsync(sources);
                Console.WriteLine($"Added {sources.Length} contribution sources.");
            }

            // ============================================
            // SEED BANK ACCOUNTS IF MISSING
            // ============================================
            var existingBankAccounts = await context.BankAccounts.CountAsync();
            if (existingBankAccounts == 0)
            {
                var accounts = new[]
                {
                    new BankAccount
                    {
                        Id = Guid.NewGuid(),
                        BankName = "Zanaco",
                        AccountName = "United Church of Zambia - Main",
                        AccountNumber = "1234567890",
                        Branch = "Lusaka Main",
                        AccountType = "CURRENT",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new BankAccount
                    {
                        Id = Guid.NewGuid(),
                        BankName = "Stanbic Bank",
                        AccountName = "United Church of Zambia - Mission Fund",
                        AccountNumber = "0987654321",
                        Branch = "Lusaka Central",
                        AccountType = "SAVINGS",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new BankAccount
                    {
                        Id = Guid.NewGuid(),
                        BankName = "ABC Bank",
                        AccountName = "United Church of Zambia - Building Fund",
                        AccountNumber = "5678901234",
                        Branch = "Lusaka East",
                        AccountType = "FIXED_DEPOSIT",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    },
                    new BankAccount
                    {
                        Id = Guid.NewGuid(),
                        BankName = "Indo Zambia Bank",
                        AccountName = "United Church of Zambia - Youth Ministry",
                        AccountNumber = "4321098765",
                        Branch = "Lusaka West",
                        AccountType = "CURRENT",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        Version = 1
                    }
                };
                await context.BankAccounts.AddRangeAsync(accounts);
                Console.WriteLine($"Added {accounts.Length} bank accounts.");
            }

            // ============================================
            // SAVE CHANGES
            // ============================================
            await context.SaveChangesAsync();

            Console.WriteLine("============================================");
            Console.WriteLine("Database seeding completed successfully!");
            Console.WriteLine("============================================");
            Console.WriteLine($"   - Sections: {await context.Sections.CountAsync()}");
            Console.WriteLine($"   - Groups: {await context.Groups.CountAsync()}");
            Console.WriteLine($"   - Committees: {await context.Committees.CountAsync()}");
            Console.WriteLine($"   - Users: {await context.Users.CountAsync()}");
            Console.WriteLine($"   - System Configs: {await context.SystemConfigs.CountAsync()}");
            Console.WriteLine($"   - Contribution Types: {await context.ContributionTypes.CountAsync()}");
            Console.WriteLine($"   - Contribution Sources: {await context.ContributionSources.CountAsync()}");
            Console.WriteLine($"   - Bank Accounts: {await context.BankAccounts.CountAsync()}");
            Console.WriteLine($"   - Fund Balances: {await context.FundBalances.CountAsync()}");
            Console.WriteLine("============================================");
        }
    }
}
