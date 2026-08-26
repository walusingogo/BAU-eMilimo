using BCryptHash = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UCP.API.Platform;
using UCP.API.Services;
using UCP.Core.Configuration;
using UCP.Core.Entities;
using UCP.Core.Enums;
using UCP.Infrastructure.Data;

namespace UCP.API.Controllers;

[ApiController, Route("api/setup"), AllowAnonymous]
public sealed class SetupController(AppDbContext db, IOptions<SecuritySettings> security, IConfiguration configuration, IWebHostEnvironment environment, IDataProtectionProvider protection, IPlatformNode platformNode) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status() => Ok(new
    {
        completed = await IsComplete(),
        hasExistingData = await db.Users.CountAsync() > 1 || await db.Groups.AnyAsync() || await db.Sections.AnyAsync() || await db.Contributions.AnyAsync() || await db.Requisitions.AnyAsync(),
        version = "1.0.0"
    });

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(SetupRequest request)
    {
        if (await IsComplete()) return Conflict(new { message = "Initial setup has already been completed." });
        if (!IsAuthorizedBootstrap(request.SetupToken))
            return Unauthorized(new { message = "The setup authorization code is invalid." });
        if (request.Mode is not ("new" or "existing")) return BadRequest(new { message = "Choose a new or existing installation." });
        if (string.IsNullOrWhiteSpace(request.ChurchName)) return BadRequest(new { message = "Organisation name is required." });
        var admin = await db.Users.FirstOrDefaultAsync(x => x.Role == UserRole.SuperAdmin.ToString());
        if (admin == null) return BadRequest(new { message = "The existing database has no SuperAdmin account." });
        if (request.Mode == "new")
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
                !System.Net.Mail.MailAddress.TryCreate(request.Email?.Trim(), out var email))
                return BadRequest(new { message = "Enter the SuperAdmin name and a valid email address." });
            var passwordError = PasswordPolicyValidator.Validate(request.Password ?? "", security.Value.PasswordPolicy);
            if (passwordError != null) return BadRequest(new { message = passwordError });
            admin.Title = request.Title?.Trim() ?? ""; admin.FirstName = request.FirstName.Trim(); admin.LastName = request.LastName.Trim();
            admin.Gender = request.Gender?.Trim() ?? ""; admin.Email = email.Address.ToLowerInvariant(); admin.PhoneNumber = request.PhoneNumber?.Trim() ?? "";
            admin.PasswordHash = BCryptHash.HashPassword(request.Password); admin.Status = UserStatus.ACTIVE.ToString(); admin.Locked = false;
            admin.PasswordExpired = false; admin.PasswordAttempts = 0; admin.UpdatedDate = DateTime.UtcNow; admin.Version++;
        }
        else if (admin.Locked || admin.Status != UserStatus.ACTIVE.ToString())
            return BadRequest(new { message = "Activate an existing SuperAdmin account before adopting this database." });
        await Upsert("church_name", request.ChurchName.Trim(), "Organisation name displayed throughout UCP");
        await Upsert("frontend_base_url", request.FrontendBaseUrl?.Trim() ?? "http://localhost:4200", "Public frontend URL used in links");
        await Upsert("setup_completed", "true", "Whether the Version 1 first-run setup has completed");
        await Upsert("system_version", "1.0.0", "Installed eMilimo product version");
        if (request.Mode == "new" && !await db.SystemLicenses.AnyAsync())
        {
            var systemIdentity = string.Equals(configuration["License:IdentityMode"], "Deployment", StringComparison.OrdinalIgnoreCase)
                ? await platformNode.GetDeploymentIdentityAsync()
                : Environment.MachineName;
            var trialXml = new XDocument(new XElement("LocalTrial",
                new XElement("IssuedAtUtc", DateTimeOffset.UtcNow.ToString("O")),
                new XElement("UserSystem", systemIdentity),
                new XElement("ExpirationDate", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd"))));
            var protector = protection.CreateProtector("UCP.SystemLicense.Payload.v1");
            db.SystemLicenses.Add(new SystemLicense
            {
                Id = Guid.NewGuid(), ProtectedPayload = protector.Protect(trialXml.ToString(SaveOptions.DisableFormatting)),
                InstalledAtUtc = DateTime.UtcNow, InstalledById = admin.Id, CreatedById = admin.Id,
                UpdatedById = admin.Id, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
            });
        }
        await db.SaveChangesAsync();
        return Ok(new { message = "eMilimo Version 1 setup completed.", next = "/login" });
    }

    private async Task<bool> IsComplete() => string.Equals(await db.SystemConfigs.Where(x => x.ConfigKey == "setup_completed").Select(x => x.ConfigValue).FirstOrDefaultAsync(), "true", StringComparison.OrdinalIgnoreCase);
    private bool IsAuthorizedBootstrap(string? suppliedToken)
    {
        var configuredToken = configuration["Setup:BootstrapToken"];
        if (string.IsNullOrWhiteSpace(configuredToken))
            return environment.IsDevelopment() && HttpContext.Connection.RemoteIpAddress is { } address && System.Net.IPAddress.IsLoopback(address);

        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(configuredToken));
        var supplied = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(expected, supplied);
    }
    private async Task Upsert(string key,string value,string description){var row=await db.SystemConfigs.FirstOrDefaultAsync(x=>x.ConfigKey==key);if(row==null)db.SystemConfigs.Add(new(){Id=Guid.NewGuid(),ConfigKey=key,ConfigValue=value,Description=description,CreatedDate=DateTime.UtcNow,UpdatedDate=DateTime.UtcNow,Version=1});else{row.ConfigValue=value;row.Description=description;row.UpdatedDate=DateTime.UtcNow;row.Version++;}}
    public sealed record SetupRequest(string Mode,string ChurchName,string? FrontendBaseUrl,string? Title,string? FirstName,string? LastName,string? Gender,string? Email,string? PhoneNumber,string? Password,string? SetupToken);
}
