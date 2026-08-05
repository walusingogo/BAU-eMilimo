using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UCP.API.Services;
using UCP.Core.Services;
using UCP.Core.Configuration;
using UCP.Infrastructure.Data;
using UCP.Infrastructure.SeedData;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));

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
        policy.WithOrigins("http://localhost:4200")
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

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IGroupAccessService, GroupAccessService>();
builder.Services.AddScoped<IRequisitionService, RequisitionService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IContributionService, ContributionService>();

// ----- Swagger with JWT Support -----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "United Church Platform API",
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

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString)
               .UseSnakeCaseNamingConvention();
    }
    else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException($"Unsupported database provider: {provider}");
    }
});

builder.Services.AddScoped<UCP.API.Services.IRolePermissionService, UCP.API.Services.RolePermissionService>();
builder.Services.AddSingleton<UCP.API.Services.ISystemLogWriter, UCP.API.Services.SystemLogWriter>();
builder.Services.AddHostedService<UCP.API.Services.ApplicationLifecycleLogger>();

// ----- JWT Authentication -----
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret is not configured.");
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UCP API V1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<UCP.API.Middleware.ProcessLoggingMiddleware>();
app.UseAuthorization();
app.UseMiddleware<UCP.API.Middleware.SessionValidationMiddleware>();

app.MapControllers();

app.Run();