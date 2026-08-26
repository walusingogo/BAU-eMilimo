using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UCP.API.Services;
using UCP.Core.Services;
using UCP.Core.Configuration;
using UCP.Core.Entities;
using UCP.Infrastructure.Data;
using UCP.Infrastructure.SeedData;
using UCP.API.Platform;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// Ensure wwwroot exists
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);

// ============================================
// CONFIGURE SERVICES
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.Port != 4200) return false;
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (!System.Net.IPAddress.TryParse(uri.Host, out var ip)) return false;
            var bytes = ip.GetAddressBytes();
            return System.Net.IPAddress.IsLoopback(ip) || bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
var configuredSharedDataPath = builder.Configuration["Platform:SharedDataPath"];
var sharedDataPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredSharedDataPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "shared-data")
    : Path.IsPathRooted(configuredSharedDataPath) ? configuredSharedDataPath : Path.Combine(builder.Environment.ContentRootPath, configuredSharedDataPath));
Directory.CreateDirectory(sharedDataPath);
builder.Services.AddDataProtection()
    .SetApplicationName(builder.Configuration["Platform:ApplicationName"] ?? "BAU.BusinessPlatform")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(sharedDataPath, "data-protection-keys")));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IGroupAccessService, GroupAccessService>();
builder.Services.AddScoped<IRequisitionService, RequisitionService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<IMembershipIdGenerator, MembershipIdGenerator>();
builder.Services.AddHttpClient("communications");
builder.Services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddSingleton<IDirectoryAuthenticationSettingsProvider, DirectoryAuthenticationSettingsProvider>();
builder.Services.AddScoped<IDirectoryAuthenticationService, DirectoryAuthenticationService>();
builder.Services.AddSingleton<IPlatformModuleCatalog, ConfigurationPlatformModuleCatalog>();
builder.Services.AddSingleton<IPlatformNode, PlatformNodeService>();
builder.Services.AddSingleton<IFileStorage, FileSystemStorage>();
builder.Services.AddSingleton<IDistributedLock, PostgreSqlDistributedLock>();
var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>() ?? new RedisSettings();
builder.Services.AddSingleton(redisSettings);
if (redisSettings.Enabled)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisSettings.ConnectionString;
        options.InstanceName = redisSettings.InstanceName;
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddHealthChecks()
    .AddCheck<PlatformHealthCheck>("platform-ready")
    .AddCheck<RedisHealthCheck>("redis-ready");

// ----- Swagger with JWT Support -----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "eMilimo API",
        Version = "v1",
        Description = "Enterprise Church Management System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token. Example: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ----- Database Configuration (Provider-agnostic) -----
var provider = builder.Configuration["Database:Provider"] ?? "PostgreSQL";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be supplied by the installer, environment variables or a protected configuration provider.");

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("eMilimo Version 1 supports PostgreSQL only.");
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

builder.Services.AddScoped<UCP.API.Services.IRolePermissionService, UCP.API.Services.RolePermissionService>();
builder.Services.AddScoped<UCP.API.Services.IApprovalNotificationService, UCP.API.Services.ApprovalNotificationService>();
builder.Services.AddSingleton<UCP.API.Services.ISystemLogWriter, UCP.API.Services.SystemLogWriter>();
builder.Services.AddHostedService<UCP.API.Services.ApplicationLifecycleLogger>();
builder.Services.AddSingleton<ISmtpSettingsProvider, SmtpSettingsProvider>();
builder.Services.AddSingleton<IEmailDeliveryQueue, EmailDeliveryQueue>();
builder.Services.AddHostedService<EmailDeliveryWorker>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IEmailService>(sp => sp.GetRequiredService<EmailService>());
builder.Services.AddScoped<ILicenseValidationService, LicenseValidationService>();
builder.Services.AddScoped<ILicenseKeyRotationService, LicenseKeyRotationService>();
builder.Services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
builder.Services.AddHostedService<DatabaseBackupScheduler>();

// ----- JWT Authentication -----
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new InvalidOperationException("Jwt:Secret must be supplied securely and contain at least 32 characters.");
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// ============================================
// SEED DATABASE ON STARTUP (Async)
// ============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        var legacyLicenseRows = await db.SystemConfigs
            .Where(x => x.ConfigKey == "license_xml" || x.ConfigKey == "license_xml_pending" || x.ConfigKey == "license_installed_at")
            .ToListAsync();
        var legacyXml = legacyLicenseRows.FirstOrDefault(x => x.ConfigKey == "license_xml")?.ConfigValue;
        if (!await db.SystemLicenses.AnyAsync() && !string.IsNullOrWhiteSpace(legacyXml))
        {
            var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("UCP.SystemLicense.Payload.v1");
            db.SystemLicenses.Add(new SystemLicense
            {
                Id = Guid.NewGuid(),
                ProtectedPayload = protector.Protect(legacyXml),
                InstalledAtUtc = DateTime.UtcNow
            });
        }
        if (legacyLicenseRows.Count > 0)
            db.SystemConfigs.RemoveRange(legacyLicenseRows);
        if (legacyLicenseRows.Count > 0 || db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();
        if (!await db.SmtpSettings.AnyAsync())
        {
            var bootstrap = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
            db.SmtpSettings.Add(new SmtpSetting
            {
                Id = Guid.NewGuid(), Enabled = bootstrap.Enabled, Host = bootstrap.Host,
                Port = bootstrap.Port, EnableSsl = bootstrap.EnableSsl, Username = bootstrap.Username,
                Password = bootstrap.Password, FromEmail = bootstrap.FromEmail, FromName = bootstrap.FromName,
                TimeoutMilliseconds = bootstrap.TimeoutMilliseconds
            });
            await db.SaveChangesAsync();
        }
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database seeding failed: {ex.Message}");
    }
}

// ============================================
// MIDDLEWARE PIPELINE
// ============================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "eMilimo API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseRouting();
app.UseMiddleware<UCP.API.Middleware.ModuleAvailabilityMiddleware>();

app.UseAuthentication();
app.UseMiddleware<UCP.API.Middleware.LicenseEnforcementMiddleware>();
app.UseMiddleware<UCP.API.Middleware.ProcessLoggingMiddleware>();
app.UseAuthorization();
app.UseMiddleware<UCP.API.Middleware.SessionValidationMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

app.Run();
