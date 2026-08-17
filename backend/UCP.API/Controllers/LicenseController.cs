using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using UCP.Core.Entities;
using UCP.Core.Services;
using UCP.Infrastructure.Data;
using UCP.API.Services;

namespace UCP.API.Controllers;

public sealed class LicenseInstallRequest
{
    public required IFormFile LicenseFile { get; init; }
}

[ApiController, Route("api/license"), Authorize]
public class LicenseController(AppDbContext db, ICurrentUserService current, IDataProtectionProvider protection, ILicenseValidationService licenseValidation) : ControllerBase
{
    private readonly IDataProtector _protector = protection.CreateProtector("UCP.SystemLicense.Payload.v1");
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        if (!IsAdministrator()) return Forbid();
        return Ok(await licenseValidation.GetStateAsync());
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

        // A new upload always replaces the current license, including an
        // invalid upload. Remove all legacy copies so the key cannot appear
        // through the generic System Config API.
        var legacy = await db.SystemConfigs.Where(x => x.ConfigKey == "license_xml" || x.ConfigKey == "license_xml_pending" || x.ConfigKey == "license_installed_at").ToListAsync();
        db.SystemConfigs.RemoveRange(legacy);
        await db.SaveChangesAsync();

        return Ok(await licenseValidation.GetStateAsync());
    }

    private bool IsAdministrator()=>current.GetUserRole() is "Admin" or "SuperAdmin";
}
