using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using UCP.API.Platform;
using UCP.Infrastructure.Data;

namespace UCP.API.Services;

public record LicenseState(bool Installed, bool IsAuthentic, bool IsValid, bool IsPerpetual, string Status,
    string Customer, string System, string ExpectedUserIdentifier, string ExpectedUserSystem, string ExpirationDate,
    DateTime? ExpiresAtUtc, int? DaysRemaining, long? SecondsRemaining, string ContactEmail, string ContactPhone);

public interface ILicenseValidationService { Task<LicenseState> GetStateAsync(); }

public sealed class LicenseValidationService(AppDbContext db, IConfiguration config, IDataProtectionProvider protection, IPlatformNode platformNode) : ILicenseValidationService
{
    private static readonly HashSet<string> TrustedKeyIds = new(StringComparer.Ordinal)
    {
        "bau-license-2026-01",
        "ucp-license-2026-01"
    };
    private readonly IDataProtector _protector = protection.CreateProtector("UCP.SystemLicense.Payload.v1");

    public async Task<LicenseState> GetStateAsync()
    {
        var email = config["License:ContactEmail"] ?? "support@bausolutions.africa";
        var phone = config["License:ContactPhone"] ?? "";
        var identifier = await Value("license_user_identifier") ?? "";
        var churchName = await Value("church_name") ?? "Church Administration";
        var system = string.Equals(config["License:IdentityMode"], "Deployment", StringComparison.OrdinalIgnoreCase)
            ? await platformNode.GetDeploymentIdentityAsync() : Environment.MachineName;
        var xml = await ReadPayloadAsync();
        if (string.IsNullOrWhiteSpace(xml)) return State(false, false, false, false, "NOT_INSTALLED", churchName, "", identifier, system, "", null, email, phone);

        try
        {
            var license = XDocument.Parse(xml, LoadOptions.None).Root?.Element("License") ?? throw new InvalidDataException();
            if (license.Attribute("Version")?.Value != "2")
            {
                if (config.GetValue("License:AllowLegacyLicenses", false)) return ValidateLegacy(license, churchName, identifier, system, email, phone);
                return State(true, false, false, false, "LEGACY_LICENSE_REISSUE_REQUIRED", churchName, "", identifier, system, "", null, email, phone);
            }

            var licenseId = Required(license, "LicenseId");
            var issuedAt = Required(license, "IssuedAtUtc");
            var customer = Required(license, "UserIdentifier");
            var licensedSystem = Required(license, "UserSystem");
            var type = Required(license, "LicenseType");
            var expiryText = Required(license, "ExpirationDate");
            var keyId = Required(license, "KeyId");
            var algorithm = Required(license, "SignatureAlgorithm");
            var signature = Convert.FromBase64String(Required(license, "Signature"));
            if (!Guid.TryParseExact(licenseId, "D", out _) || !DateTimeOffset.TryParseExact(issuedAt, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new InvalidDataException();
            if (!TrustedKeyIds.Contains(keyId) || algorithm != "RSA-PSS-SHA256") throw new CryptographicException();

            var perpetual = type.Equals("Perpetual", StringComparison.Ordinal) && expiryText == "PERPETUAL";
            var dated = DateOnly.TryParseExact(expiryText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry);
            if (!perpetual && (!type.Equals("Term", StringComparison.Ordinal) || !dated)) throw new InvalidDataException();
            var payload = Canonical(licenseId, issuedAt, customer, licensedSystem, type, expiryText, keyId);
            using var rsa = LoadPublicKey();
            var signed = rsa.VerifyData(Encoding.UTF8.GetBytes(payload), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            var identityMatches = identifier.Length > 0 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(customer), Encoding.UTF8.GetBytes(identifier));
            var systemMatches = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(licensedSystem.ToUpperInvariant()), Encoding.UTF8.GetBytes(system.ToUpperInvariant()));
            var authentic = signed && identityMatches && systemMatches;
            DateTime? end = perpetual ? null : new DateTime(expiry.Year, expiry.Month, expiry.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
            var remaining = perpetual ? TimeSpan.MaxValue : end!.Value - DateTime.UtcNow;
            var expired = !perpetual && remaining <= TimeSpan.Zero;
            var status = !signed ? "INVALID_SIGNATURE" : !identityMatches ? "IDENTIFIER_MISMATCH" : !systemMatches ? "SYSTEM_MISMATCH" : expired ? "EXPIRED" : !perpetual && remaining.TotalDays <= 30 ? "EXPIRING_SOON" : "ACTIVE";
            return State(true, authentic, authentic && !expired, perpetual, status, churchName, licensedSystem, identifier, system,
                perpetual ? "PERPETUAL" : expiryText, end, email, phone, remaining);
        }
        catch
        {
            return State(true, false, false, false, "INVALID", churchName, "", identifier, system, "", null, email, phone);
        }
    }

    private LicenseState ValidateLegacy(XElement license, string churchName, string identifier, string system, string email, string phone)
    {
        var customer = license.Element("UserIdentifier")?.Value.Trim() ?? "";
        var licensedSystem = license.Element("UserSystem")?.Value.Trim() ?? "";
        var expiryText = license.Element("ExpirationDate")?.Value.Trim() ?? "";
        var type = license.Element("LicenseType")?.Value.Trim() ?? "Term";
        var key = license.Element("LicenseKey")?.Value.Trim() ?? "";
        var perpetual = type.Equals("Perpetual", StringComparison.OrdinalIgnoreCase) || expiryText == "PERPETUAL";
        var dated = DateOnly.TryParseExact(expiryText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry);
        if ((!perpetual && !dated) || key.Length <= 44) throw new InvalidDataException();
        var raw = key[..^44]; var hash = key[^44..];
        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var term = perpetual ? "PERPETUAL" : expiry.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var valid = CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(hash)) && raw.StartsWith(customer + term, StringComparison.Ordinal) && raw.EndsWith("$" + licensedSystem, StringComparison.Ordinal);
        var matches = customer == identifier && licensedSystem.Equals(system, StringComparison.OrdinalIgnoreCase);
        DateTime? end = perpetual ? null : new DateTime(expiry.Year, expiry.Month, expiry.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var remaining = perpetual ? TimeSpan.MaxValue : end!.Value - DateTime.UtcNow;
        return State(true, valid && matches, valid && matches && remaining > TimeSpan.Zero, perpetual, "LEGACY_ACTIVE_REISSUE_REQUIRED", churchName, licensedSystem, identifier, system, expiryText, end, email, phone, remaining);
    }

    private static RSA LoadPublicKey()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UCP.API.Licensing.bau-license-public.pem") ?? throw new CryptographicException("License verification key is unavailable.");
        using var reader = new StreamReader(stream);
        var rsa = RSA.Create(); rsa.ImportFromPem(reader.ReadToEnd()); return rsa;
    }

    private static string Canonical(string id, string issued, string customer, string system, string type, string expiry, string keyId) =>
        string.Join('.', new[] { "BAU-LICENSE-V2", id, issued, customer, system, type, expiry, keyId }.Select(x => Convert.ToBase64String(Encoding.UTF8.GetBytes(x))));
    private static string Required(XElement license, string name)
    {
        var value = license.Element(name)?.Value.Trim() ?? "";
        if (value.Length is 0 or > 2000 || value.Any(char.IsControl)) throw new InvalidDataException();
        return value;
    }
    private static LicenseState State(bool installed, bool authentic, bool valid, bool perpetual, string status, string customer, string licensedSystem, string expectedIdentifier, string expectedSystem, string expiry, DateTime? end, string email, string phone, TimeSpan? remaining = null) =>
        new(installed, authentic, valid, perpetual, status, customer, licensedSystem, expectedIdentifier, expectedSystem, expiry, end,
            perpetual ? null : Math.Max(0, (int)Math.Ceiling((remaining ?? TimeSpan.Zero).TotalDays)), perpetual ? null : Math.Max(0, (long)(remaining ?? TimeSpan.Zero).TotalSeconds), email, phone);
    private Task<string?> Value(string key) => db.SystemConfigs.AsNoTracking().Where(x => !x.IsArchived && x.ConfigKey == key).Select(x => x.ConfigValue).FirstOrDefaultAsync();
    private async Task<string?> ReadPayloadAsync()
    {
        var protectedPayload = await db.SystemLicenses.AsNoTracking().OrderByDescending(x => x.InstalledAtUtc).Select(x => x.ProtectedPayload).FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(protectedPayload)) { try { return _protector.Unprotect(protectedPayload); } catch { return "<invalid-protected-license/>"; } }
        return await Value("license_xml");
    }
}
