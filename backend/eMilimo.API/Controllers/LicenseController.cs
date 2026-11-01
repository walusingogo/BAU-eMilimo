using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using eMilimo.API.Services;

namespace eMilimo.API.Controllers;

public sealed class LicenseInstallRequest
{
    public required IFormFile LicenseFile { get; init; }
}

[ApiController, Route("api/license"), Authorize]
public class LicenseController(AppDbContext db, ICurrentUserService current, IDataProtectionProvider protection, ILicenseValidationService licenseValidation, ILicenseKeyRotationService rotations) : ControllerBase
{
    private readonly IDataProtector _protector = protection.CreateProtector("eMilimo.SystemLicense.Payload.v1");
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (!IsAdministrator()) return Forbid();
        return Ok(await licenseValidation.GetStateAsync());
    }

    [HttpGet("banner")]
    public async Task<IActionResult> Banner()
    {
        var state = await licenseValidation.GetStateAsync();
        return Ok(new { state.IsTrial, state.IsValid, state.Status, state.DaysRemaining, state.ExpirationDate });
    }

    [HttpPost("install")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Install([FromForm] LicenseInstallRequest request)
    {
        if (!IsAdministrator()) return Forbid();
        var licenseFile = request.LicenseFile;
        if (licenseFile.Length == 0 || licenseFile.Length > 1024 * 1024 || !Path.GetExtension(licenseFile.FileName).Equals(".xml", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Select a valid signed licence XML file (maximum 1 MB)." });
        string xml; using (var reader = new StreamReader(licenseFile.OpenReadStream())) xml = await reader.ReadToEndAsync();
        var expectedIdentifier = await db.SystemConfigs.AsNoTracking()
            .Where(x => !x.IsArchived && x.ConfigKey.Trim() == "license_user_identifier")
            .Select(x => x.ConfigValue).FirstOrDefaultAsync();
        var validation = await licenseValidation.ValidatePayloadAsync(xml, expectedIdentifier);
        if (!validation.IsAuthentic || !validation.IsValid)
            return BadRequest(new { message = $"The licence was not installed because validation failed ({validation.Status}).", validation.Status });

        var installed = await db.SystemLicenses.FirstOrDefaultAsync();
        if (installed == null)
        {
            installed = new SystemLicense { Id = Guid.NewGuid(), CreatedById = current.GetUserId() };
            db.SystemLicenses.Add(installed);
        }
        installed.ProtectedPayload = _protector.Protect(xml);
        installed.InstalledAtUtc = DateTime.UtcNow;
        installed.InstalledById = current.GetUserId();
        installed.UpdatedById = current.GetUserId();
        installed.UpdatedDate = DateTime.UtcNow;
        installed.Version++;

        if (string.IsNullOrWhiteSpace(expectedIdentifier))
        {
            db.SystemConfigs.Add(new SystemConfig
            {
                Id = Guid.NewGuid(), ConfigKey = "license_user_identifier", ConfigValue = validation.Customer,
                Description = "Identifier bound to the installed signed licence", CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow, Version = 1
            });
        }

        await db.SaveChangesAsync();

        return Ok(await licenseValidation.GetStateAsync());
    }

    [HttpGet("trusted-keys")]
    public async Task<IActionResult> TrustedKeys(CancellationToken cancellationToken)
    {
        if (current.GetUserRole() != "SuperAdmin") return Forbid();
        return Ok(await rotations.GetStateAsync(cancellationToken));
    }

    [HttpPost("rotate-key")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RotateKey([FromForm] LicenseRotationUpload request, CancellationToken cancellationToken)
    {
        if (current.GetUserRole() != "SuperAdmin") return Forbid();
        if (request.RotationPackage.Length is 0 or > 262144 || !Path.GetExtension(request.RotationPackage.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Select a signed BAU key-rotation JSON package (maximum 256 KB)." });
        using var reader = new StreamReader(request.RotationPackage.OpenReadStream());
        var json = await reader.ReadToEndAsync(cancellationToken);
        var userId = current.GetUserId();
        if (!userId.HasValue) return Unauthorized();
        try { return Ok(await rotations.InstallAsync(json, userId.Value, cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private bool IsAdministrator()=>current.GetUserRole() is "Admin" or "SuperAdmin";
}

public sealed class LicenseRotationUpload
{
    public required IFormFile RotationPackage { get; init; }
}
