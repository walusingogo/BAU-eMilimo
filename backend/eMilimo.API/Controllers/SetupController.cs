using BCryptHash = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using eMilimo.API.Platform;
using eMilimo.API.Services;
using eMilimo.Core.Configuration;
using eMilimo.Core.Entities;
using eMilimo.Core.Enums;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers;

[ApiController, Route("api/setup"), AllowAnonymous]
public sealed class SetupController(AppDbContext db, IOptions<SecuritySettings> security, IConfiguration configuration, IWebHostEnvironment environment, IDataProtectionProvider protection, IPlatformNode platformNode, ILicenseValidationService licenseValidation) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> Status() => Ok(new
    {
        completed = await IsComplete(),
        hasExistingData = await db.Users.CountAsync() > 1 || await db.Groups.AnyAsync() || await db.Sections.AnyAsync() || await db.Contributions.AnyAsync() || await db.Requisitions.AnyAsync(),
        congregationName = configuration["Setup:CongregationName"] ?? "",
        instance = configuration["Platform:Instance"] ?? "main",
        systemId = configuration["Platform:DeploymentId"] ?? "",
        version = "1.0.0"
    });

    [HttpPost("validate-license")]
    public async Task<IActionResult> ValidateLicense(SetupLicenseValidationRequest request)
    {
        if (await IsComplete()) return Conflict(new { message = "Initial setup has already been completed." });
        if (!IsAuthorizedBootstrap(request.SetupToken))
            return Unauthorized(new { message = "The setup authorization code is invalid." });
        if (!ValidIdentifier(request.LicenseUserIdentifier)) return BadRequest(new { message = "The installation licence identifier is invalid." });
        var state = await licenseValidation.ValidatePayloadAsync(request.LicenseXml, request.LicenseUserIdentifier);
        if (!state.IsAuthentic || !state.IsValid)
            return BadRequest(new { message = LicenseError(state.Status), state.Status });
        return Ok(new { state.Status, state.Customer, state.System, state.ExpirationDate, state.IsPerpetual, state.IsTrial });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(SetupRequest request)
    {
        if (await IsComplete()) return Conflict(new { message = "Initial setup has already been completed." });
        if (!IsAuthorizedBootstrap(request.SetupToken))
            return Unauthorized(new { message = "The setup authorization code is invalid." });
        if (request.Mode is not ("new" or "existing")) return BadRequest(new { message = "Choose a new or existing installation." });
        if (string.IsNullOrWhiteSpace(request.ChurchName)) return BadRequest(new { message = "Organisation name is required." });
        if (string.IsNullOrWhiteSpace(request.Instance)) return BadRequest(new { message = "Instance is required." });
        if (string.IsNullOrWhiteSpace(request.SystemId)) return BadRequest(new { message = "System ID is required." });
        if (!ValidIdentifier(request.LicenseUserIdentifier)) return BadRequest(new { message = "The installation licence identifier is invalid." });
        LicenseState? suppliedLicense = null;
        if (!string.IsNullOrWhiteSpace(request.LicenseXml))
        {
            suppliedLicense = await licenseValidation.ValidatePayloadAsync(request.LicenseXml, request.LicenseUserIdentifier);
            if (!suppliedLicense.IsAuthentic || !suppliedLicense.IsValid)
                return BadRequest(new { message = LicenseError(suppliedLicense.Status), suppliedLicense.Status });
        }
        else if (!request.UseTrial && !await db.SystemLicenses.AnyAsync())
            return BadRequest(new { message = "Choose a signed BAU licence or the 30-day trial." });
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
        await Upsert("church_name", request.ChurchName.Trim(), "Organisation name displayed throughout eMilimo");
        await Upsert("instance", request.Instance.Trim(), "Configured eMilimo instance name");
        await Upsert("system_id", request.SystemId.Trim(), "Configured eMilimo system identifier");
        await Upsert("frontend_base_url", request.FrontendBaseUrl?.Trim() ?? "http://localhost:4200", "Public frontend URL used in links");
        await Upsert("giving_base_url", request.GivingBaseUrl?.Trim() ?? "http://localhost:9200", "Public Digital Giving URL used in links and receipts");
        await Upsert("setup_completed", "true", "Whether the Version 1 first-run setup has completed");
        await Upsert("system_version", "1.0.0", "Installed eMilimo product version");
        await Upsert("license_user_identifier", request.LicenseUserIdentifier.ToLowerInvariant(), "Unique immutable identifier generated for this eMilimo installation");
        if (suppliedLicense is not null)
        {
            await StoreLicense(request.LicenseXml!, admin.Id);
        }
        else if (request.Mode == "new" && request.UseTrial)
        {
            var systemIdentity = string.Equals(configuration["License:IdentityMode"], "Deployment", StringComparison.OrdinalIgnoreCase)
                ? await platformNode.GetDeploymentIdentityAsync()
                : Environment.MachineName;
            var trialXml = new XDocument(new XElement("LocalTrial",
                new XElement("IssuedAtUtc", DateTimeOffset.UtcNow.ToString("O")),
                new XElement("UserSystem", systemIdentity),
                new XElement("ExpirationDate", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd"))));
            await StoreLicense(trialXml.ToString(SaveOptions.DisableFormatting), admin.Id);
        }
        await db.SaveChangesAsync();
        return Ok(new
        {
            message = "eMilimo Version 1 setup completed.", next = "/login",
            credentials = request.Mode == "new" ? new { email = admin.Email, password = request.Password } : null
        });
    }

    private async Task StoreLicense(string xml, Guid administratorId)
    {
        var protector = protection.CreateProtector("eMilimo.SystemLicense.Payload.v1");
        var row = await db.SystemLicenses.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new SystemLicense { Id = Guid.NewGuid(), CreatedById = administratorId, CreatedDate = DateTime.UtcNow };
            db.SystemLicenses.Add(row);
        }
        row.ProtectedPayload = protector.Protect(xml);
        row.InstalledAtUtc = DateTime.UtcNow;
        row.InstalledById = administratorId;
        row.UpdatedById = administratorId;
        row.UpdatedDate = DateTime.UtcNow;
        row.Version++;
    }

    private static string LicenseError(string status) => status switch
    {
        "INVALID_SIGNATURE" => "The licence signature is invalid. Request a new signed licence from BAU Solutions.",
        "UNTRUSTED_SIGNING_KEY" => "The licence was issued with a signing key that this eMilimo version does not trust.",
        "UNSUPPORTED_SIGNATURE_ALGORITHM" => "The licence uses an unsupported signature algorithm.",
        "SYSTEM_MISMATCH" => "This licence was issued for a different eMilimo system.",
        "IDENTIFIER_MISMATCH" => "This licence was issued for a different eMilimo installation identifier.",
        "EXPIRED" => "This licence has expired.",
        _ => "The selected file is not a valid signed eMilimo licence."
    };

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
    private static bool ValidIdentifier(string? value) => value is { Length: 32 } && value.All(char.IsAsciiHexDigit);
    private async Task Upsert(string key,string value,string description){var row=await db.SystemConfigs.FirstOrDefaultAsync(x=>x.ConfigKey.Trim()==key);if(row==null)db.SystemConfigs.Add(new(){Id=Guid.NewGuid(),ConfigKey=key,ConfigValue=value,Description=description,CreatedDate=DateTime.UtcNow,UpdatedDate=DateTime.UtcNow,Version=1});else{row.ConfigKey=key;row.ConfigValue=value;row.Description=description;row.IsArchived=false;row.UpdatedDate=DateTime.UtcNow;row.Version++;}}
    public sealed record SetupLicenseValidationRequest(string LicenseXml, string? SetupToken, string? LicenseUserIdentifier);
    public sealed record SetupRequest(string Mode,string ChurchName,string Instance,string SystemId,string LicenseUserIdentifier,string? FrontendBaseUrl,string? GivingBaseUrl,string? Title,string? FirstName,string? LastName,string? Gender,string? Email,string? PhoneNumber,string? Password,string? SetupToken,string? LicenseXml,bool UseTrial = true);
}
