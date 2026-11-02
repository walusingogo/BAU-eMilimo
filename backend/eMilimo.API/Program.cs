using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using eMilimo.API.Services;
using eMilimo.API.Payments;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Core.Configuration;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using eMilimo.Infrastructure.SeedData;
using eMilimo.API.Platform;
using BAU.Platform.Identity.AspNetCore;
using BAU.Platform.Authorization.AspNetCore;
using BAU.Platform.Observability.AspNetCore;
using eMilimo.API.Authorization;
using BAU.Platform.Notifications;
using BAU.Platform.Messaging;
using BAU.Platform.Messaging.Dispatching;
using BAU.Platform.Messaging.Hosting;
using BAU.Platform.Abstractions.Application;
using BAU.Platform.Outbox.EntityFrameworkCore;
using eMilimo.API.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<SecuritySettings>()
    .Bind(builder.Configuration.GetSection("Security"))
    .Validate(x => x.PasswordPolicy.MinimumLength >= 8 &&
        x.PasswordPolicy.MaximumLength >= x.PasswordPolicy.MinimumLength &&
        x.PasswordPolicy.ExpiryDays > 0,
        "Security:PasswordPolicy values are invalid.")
    .Validate(x => x.Session.LifetimeMinutes > 0 && x.Session.IdleTimeoutMinutes > 0 &&
        x.Session.WarningCountdownSeconds > 0,
        "Security:Session values must be positive.")
    .Validate(x => x.Session.IdleTimeoutMinutes <= x.Session.LifetimeMinutes &&
        x.Session.WarningCountdownSeconds < x.Session.IdleTimeoutMinutes * 60,
        "Security:Session idle timeout cannot exceed token lifetime, and the warning must be shorter than the idle timeout.")
    .ValidateOnStart();
builder.Services.AddOptions<UploadSettings>()
    .Bind(builder.Configuration.GetSection("Uploads"))
    .Validate(x => x.MaximumFileSizeBytes is >= 1024 and <= 104857600,
        "Uploads:MaximumFileSizeBytes must be between 1 KB and 100 MB.")
    .Validate(x => x.AllowedExtensions.Length > 0 &&
        x.AllowedExtensions.All(extension => extension.StartsWith('.') && extension == extension.ToLowerInvariant()),
        "Uploads:AllowedExtensions must contain lower-case extensions beginning with a dot.")
    .Validate(x => x.AllowedContentTypes.Length > 0,
        "Uploads:AllowedContentTypes must contain at least one MIME type.")
    .ValidateOnStart();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// Ensure wwwroot exists
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);

// ============================================
// CONFIGURE SERVICES
// ============================================
builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    var allowedOrigins = configuredOrigins
        .Where(x => Uri.TryCreate(x, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        .Select(x => x.Trim().TrimEnd('/'))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            var normalized = origin.Trim().TrimEnd('/');
            if (allowedOrigins.Contains(normalized)) return true;
            if (!builder.Environment.IsDevelopment() ||
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.Port is not (4200 or 9200)) return false;
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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddBauHttpDiagnostics();
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
builder.Services.AddBauCurrentActor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IGroupAccessService, GroupAccessService>();
builder.Services.AddScoped<IRequisitionService, RequisitionService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IContributionService, ContributionService>();
builder.Services.AddScoped<IContributionPostingService, ContributionPostingService>();
builder.Services.AddScoped<IAccountingIntegrationService, AccountingIntegrationService>();
builder.Services.AddScoped<IAccountingExportAdapter, SagePastelFileExportAdapter>();
builder.Services.AddHostedService<AccountingImportResponseWorker>();
builder.Services.AddScoped<IServiceCollectionContributionPostingService, ServiceCollectionContributionPostingService>();
builder.Services.AddScoped<IContributionDestinationValidationService, ContributionDestinationValidationService>();
builder.Services.AddScoped<IOrganizationOverviewService, OrganizationOverviewService>();
builder.Services.AddSingleton<IPaymentProvider, AirtelMoneyPaymentProvider>();
builder.Services.AddSingleton<IPaymentProvider, MtnMomoPaymentProvider>();
builder.Services.AddSingleton<IPaymentProvider, ZamtelMoneyPaymentProvider>();
builder.Services.AddSingleton<IPaymentProvider, DevelopmentSandboxPaymentProvider>();
builder.Services.AddSingleton<IPaymentProviderRegistry, PaymentProviderRegistry>();
builder.Services.AddSingleton<PaymentProviderEndpointPolicy>();
builder.Services.AddSingleton<IPaymentApiExchangeWriter, PaymentApiExchangeWriter>();
builder.Services.AddScoped<IPaymentProviderConfigurationStore, PaymentProviderConfigurationStore>();
builder.Services.AddScoped<IPaymentRoutingService, PaymentRoutingService>();
builder.Services.AddScoped<IDigitalGivingConfigurationService, DigitalGivingConfigurationService>();
builder.Services.AddScoped<IMemberVerificationNotifier, MemberVerificationNotifier>();
builder.Services.AddScoped<IMemberVerificationService, MemberVerificationService>();
builder.Services.AddScoped<IDigitalPaymentIntentService, DigitalPaymentIntentService>();
builder.Services.AddScoped<IPublicPaymentStatusRefreshService, PublicPaymentStatusRefreshService>();
builder.Services.AddScoped<IPaymentWebhookProcessor, PaymentWebhookProcessor>();
builder.Services.AddScoped<IPaymentFinalizationService, PaymentFinalizationService>();
builder.Services.AddScoped<PaymentDeveloperService>();
builder.Services.AddScoped<MtnSandboxTestingService>();
builder.Services.AddHostedService<PaymentReconciliationWorker>();
builder.Services.AddHostedService<PaymentDiagnosticsRetentionWorker>();
builder.Services.AddScoped<ICashDenominationSettingsService, CashDenominationSettingsService>();
builder.Services.AddScoped<IReferenceNumberService, ReferenceNumberService>();
builder.Services.AddScoped<IFinancialRecordBookService, FinancialRecordBookService>();
builder.Services.AddScoped<IMembershipIdGenerator, MembershipIdGenerator>();
builder.Services.AddScoped<IMembershipDirectoryProvider, LocalMembershipDirectoryProvider>();
builder.Services.AddScoped<IMembershipDirectoryProvider, RestApiMembershipDirectoryProvider>();
builder.Services.AddHttpClient("membership-directory");
builder.Services.AddHttpClient("communications");
builder.Services.AddHttpClient("fcy-provider", client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHttpClient("mtn-momo", client => client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });
builder.Services.AddSingleton<IFcyProviderSettings, FcyProviderSettings>();
builder.Services.AddScoped<IFcyRateProvider, AllRatesTodayBozProvider>();
builder.Services.AddSingleton<ICommunicationSettingsProvider, CommunicationSettingsProvider>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddSingleton<IDirectoryAuthenticationSettingsProvider, DirectoryAuthenticationSettingsProvider>();
builder.Services.AddScoped<IDirectoryAuthenticationService, DirectoryAuthenticationService>();
builder.Services.AddSingleton<IPlatformModuleCatalog, ConfigurationPlatformModuleCatalog>();
builder.Services.AddSingleton<IPlatformNode, PlatformNodeService>();
builder.Services.AddSingleton<IFileStorage, FileSystemStorage>();
builder.Services.AddSingleton<IDistributedLock, PostgreSqlDistributedLock>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IOutboxStore, EfCoreOutboxStore<AppDbContext>>();
builder.Services.AddScoped<IOutboxWriter, EfCoreOutboxWriter<AppDbContext>>();
builder.Services.AddSingleton(new OutboxDispatchOptions());
builder.Services.AddScoped<OutboxDispatcher>();
builder.Services.AddBauNotificationRouting();
// Register the app's SMTP implementation as the platform email gateway.
builder.Services.AddScoped<IEmailGateway, EmailGatewayAdapter>();
builder.Services.AddSingleton<ISmsGateway, NoopSmsGateway>();
builder.Services.AddSingleton<IWhatsAppGateway, NoopWhatsAppGateway>();
// Gate the platform outbox worker until database migrations have created the platform schema.
builder.Services.AddSingleton<IOutboxDatabaseMigrator, EfCoreOutboxDatabaseMigrator>();
builder.Services.AddSingleton<IOutboxWorkerStarter, DefaultOutboxWorkerStarter>();
builder.Services.AddHostedService<DelayedOutboxWorkerHostedService>();
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

builder.Services.AddDbContextFactory<AppDbContext>((serviceProvider, options) =>
{
    if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("eMilimo Version 1 supports PostgreSQL only.");
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddScoped<eMilimo.API.Services.IRolePermissionService, eMilimo.API.Services.RolePermissionService>();
builder.Services.AddScoped<eMilimo.API.Services.IWorkflowAuthorizationService, eMilimo.API.Services.WorkflowAuthorizationService>();
builder.Services.AddScoped<IPermissionEvaluator, RolePermissionEvaluator>();
builder.Services.AddScoped<eMilimo.API.Services.IApprovalNotificationService, eMilimo.API.Services.ApprovalNotificationService>();
builder.Services.AddSingleton<eMilimo.API.Services.ISystemLogWriter, eMilimo.API.Services.SystemLogWriter>();
builder.Services.AddHostedService<eMilimo.API.Services.ApplicationLifecycleLogger>();
builder.Services.AddSingleton<ISmtpSettingsProvider, SmtpSettingsProvider>();
builder.Services.AddSingleton<IEmailDeliveryQueue, EmailDeliveryQueue>();
builder.Services.AddHostedService<EmailDeliveryWorker>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
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

builder.Services.AddBauCapabilityAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-digital-giving", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("digital-giving-otp", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("digital-giving-webhook", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseBauHttpDiagnostics();

// ============================================
// SEED DATABASE ON STARTUP (Async)
// ============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
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
        // A partially initialized database is not safe to serve. Fail once at the
        // authoritative startup migration point instead of allowing hosted services
        // to retry the same migration and obscure the original failure.
        app.Logger.LogCritical(ex, "Database migration or seeding failed during startup.");
        throw;
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

if (app.Urls.Any(url => Uri.TryCreate(url, UriKind.Absolute, out var address) &&
                        address.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");
var givingWebRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot-giving");
var givingPort = builder.Configuration.GetValue<int?>("Giving:Port");
if (givingPort is > 0 && Directory.Exists(givingWebRoot))
{
    var givingFiles = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(givingWebRoot);
    app.MapWhen(context =>
            context.Connection.LocalPort == givingPort &&
            !context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Path.StartsWithSegments("/health"),
        givingApp =>
        {
            givingApp.UseDefaultFiles(new DefaultFilesOptions { FileProvider = givingFiles });
            givingApp.UseStaticFiles(new StaticFileOptions { FileProvider = givingFiles });
            givingApp.Run(async context =>
            {
                if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                var indexPath = Path.Combine(givingWebRoot, "index.html");
                if (!File.Exists(indexPath))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.Headers.CacheControl = "no-store";
                if (!HttpMethods.IsHead(context.Request.Method))
                    await context.Response.SendFileAsync(indexPath);
            });
        });
}
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<eMilimo.API.Middleware.ModuleAvailabilityMiddleware>();

app.UseAuthentication();
app.UseMiddleware<eMilimo.API.Middleware.LicenseEnforcementMiddleware>();
app.UseMiddleware<eMilimo.API.Middleware.ProcessLoggingMiddleware>();
app.UseAuthorization();
app.UseMiddleware<eMilimo.API.Middleware.SessionValidationMiddleware>();

app.MapControllers();
app.MapGet("/setup", (IWebHostEnvironment hostEnvironment, IConfiguration configuration) =>
{
    var indexPath = Path.Combine(hostEnvironment.WebRootPath ?? Path.Combine(hostEnvironment.ContentRootPath, "wwwroot"), "index.html");
    if (File.Exists(indexPath)) return Results.File(indexPath, "text/html; charset=utf-8");
    var frontend = (configuration["Frontend:Url"] ?? "http://localhost:4200").TrimEnd('/');
    return Results.Redirect($"{frontend}/setup");
}).AllowAnonymous();
app.MapFallbackToFile("index.html");
app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

app.Run();
