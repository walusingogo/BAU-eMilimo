using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using eMilimo.API.Platform;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public record LicenseState(bool Installed, bool IsAuthentic, bool IsValid, bool IsPerpetual, bool IsTrial, string Status,
    string Customer, string System, string ExpectedUserIdentifier, string ExpectedUserSystem, string ExpirationDate,
    DateTime? ExpiresAtUtc, int? DaysRemaining, long? SecondsRemaining, string ContactEmail, string ContactPhone);

public interface ILicenseValidationService
{
    Task<LicenseState> GetStateAsync();
    Task<LicenseState> ValidatePayloadAsync(string xml, string? expectedIdentifier = null);
}

public sealed class LicenseValidationService(AppDbContext db, IConfiguration config, IDataProtectionProvider protection, IPlatformNode platformNode) : ILicenseValidationService
{
    private const string ProductCode = "eMilimo";
    private static readonly HashSet<string> TrustedKeyIds = new(StringComparer.Ordinal)
    {
        "bau-license-2026-01"
    };
    private readonly IDataProtector _protector = protection.CreateProtector("eMilimo.SystemLicense.Payload.v1");

    public async Task<LicenseState> GetStateAsync()
    {
        var email = config["License:ContactEmail"] ?? "support@bausolutions.africa";
        var phone = config["License:ContactPhone"] ?? "";
        var identifier = await Value("license_user_identifier") ?? "";
        var churchName = await Value("church_name") ?? "Church Administration";
        var system = string.Equals(config["License:IdentityMode"], "Deployment", StringComparison.OrdinalIgnoreCase)
            ? await platformNode.GetDeploymentIdentityAsync() : Environment.MachineName;
        var xml = await ReadPayloadAsync();
        if (string.IsNullOrWhiteSpace(xml)) return State(false, false, false, false, false, "NOT_INSTALLED", churchName, "", identifier, system, "", null, email, phone);

        try
        {
            var document = XDocument.Parse(xml, LoadOptions.None);
            if (document.Root?.Name.LocalName == "LocalTrial")
                return ValidateLocalTrial(document.Root, churchName, identifier, system, email, phone);
            return await ValidateSignedPayloadAsync(document, identifier, churchName, system, email, phone);
        }
        catch
        {
            return State(true, false, false, false, false, "INVALID", churchName, "", identifier, system, "", null, email, phone);
        }
    }

    public async Task<LicenseState> ValidatePayloadAsync(string xml, string? expectedIdentifier = null)
    {
        var email = config["License:ContactEmail"] ?? "support@bausolutions.africa";
        var phone = config["License:ContactPhone"] ?? "";
        var churchName = await Value("church_name") ?? "Church Administration";
        var system = string.Equals(config["License:IdentityMode"], "Deployment", StringComparison.OrdinalIgnoreCase)
            ? await platformNode.GetDeploymentIdentityAsync() : Environment.MachineName;
        try
        {
            if (string.IsNullOrWhiteSpace(xml) || Encoding.UTF8.GetByteCount(xml) > 1024 * 1024)
                throw new InvalidDataException();
            var document = XDocument.Parse(xml, LoadOptions.None);
            if (document.Root?.Name.LocalName == "LocalTrial")
                throw new InvalidDataException("Local trial payloads cannot be uploaded.");
            return await ValidateSignedPayloadAsync(document, expectedIdentifier, churchName, system, email, phone);
        }
        catch
        {
            return State(true, false, false, false, false, "INVALID", churchName, "", expectedIdentifier ?? "", system, "", null, email, phone);
        }
    }

    private async Task<LicenseState> ValidateSignedPayloadAsync(XDocument document, string? expectedIdentifier, string churchName, string system, string email, string phone)
    {
            var license = document.Root?.Element("License") ?? throw new InvalidDataException();
            if (license.Attribute("Version")?.Value != "2") throw new InvalidDataException();

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
            var keyKnown = TrustedKeyIds.Contains(keyId) || await db.LicenseTrustedKeys.AsNoTracking()
                .AnyAsync(x => x.ProductCode == ProductCode && x.KeyId == keyId);
            if (!keyKnown)
                return State(true, false, false, false, false, "UNTRUSTED_SIGNING_KEY", churchName, licensedSystem,
                    expectedIdentifier ?? "", system, expiryText, null, email, phone);
            if (algorithm != "RSA-PSS-SHA256")
                return State(true, false, false, false, false, "UNSUPPORTED_SIGNATURE_ALGORITHM", churchName, licensedSystem,
                    expectedIdentifier ?? "", system, expiryText, null, email, phone);

            var perpetual = type.Equals("Perpetual", StringComparison.Ordinal) && expiryText == "PERPETUAL";
            var trial = type.Equals("Trial", StringComparison.Ordinal);
            var dated = DateOnly.TryParseExact(expiryText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry);
            if (!perpetual && (!(type.Equals("Term", StringComparison.Ordinal) || trial) || !dated)) throw new InvalidDataException();
            var payload = Canonical(licenseId, issuedAt, customer, licensedSystem, type, expiryText, keyId);
            using var rsa = await LoadPublicKeyAsync(keyId);
            var signed = rsa.VerifyData(Encoding.UTF8.GetBytes(payload), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            var identifier = string.IsNullOrWhiteSpace(expectedIdentifier) ? customer : expectedIdentifier.Trim();
            var identityMatches = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(customer), Encoding.UTF8.GetBytes(identifier));
            var systemMatches = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(licensedSystem.ToUpperInvariant()), Encoding.UTF8.GetBytes(system.ToUpperInvariant()));
            var authentic = signed && identityMatches && systemMatches;
            DateTime? end = perpetual ? null : new DateTime(expiry.Year, expiry.Month, expiry.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
            var remaining = perpetual ? TimeSpan.MaxValue : end!.Value - DateTime.UtcNow;
            var expired = !perpetual && remaining <= TimeSpan.Zero;
            var status = !signed ? "INVALID_SIGNATURE" : !identityMatches ? "IDENTIFIER_MISMATCH" : !systemMatches ? "SYSTEM_MISMATCH" : expired ? "EXPIRED" : !perpetual && remaining.TotalDays <= 30 ? "EXPIRING_SOON" : "ACTIVE";
            return State(true, authentic, authentic && !expired, perpetual, trial, status, churchName, licensedSystem, identifier, system,
                perpetual ? "PERPETUAL" : expiryText, end, email, phone, remaining);
    }

    private static LicenseState ValidateLocalTrial(XElement trial, string churchName, string identifier, string system, string email, string phone)
    {
        var licensedSystem = Required(trial, "UserSystem");
        var expiryText = Required(trial, "ExpirationDate");
        if (!DateOnly.TryParseExact(expiryText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
            throw new InvalidDataException();
        var systemMatches = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(licensedSystem.ToUpperInvariant()), Encoding.UTF8.GetBytes(system.ToUpperInvariant()));
        var end = new DateTime(expiry.Year, expiry.Month, expiry.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        var remaining = end - DateTime.UtcNow;
        var valid = systemMatches && remaining > TimeSpan.Zero;
        return State(true, systemMatches, valid, false, true, !systemMatches ? "SYSTEM_MISMATCH" : valid ? "TRIAL" : "EXPIRED", churchName, licensedSystem, identifier, system, expiryText, end, email, phone, remaining);
    }

    private async Task<RSA> LoadPublicKeyAsync(string keyId)
    {
        var stored = await db.LicenseTrustedKeys.AsNoTracking().FirstOrDefaultAsync(x => x.ProductCode == ProductCode && x.KeyId == keyId);
        if (stored is not null)
        {
            if (stored.Status == "REVOKED" || stored.ValidFromUtc > DateTime.UtcNow) throw new CryptographicException("The licence signing key is not active.");
            var configured = RSA.Create(); configured.ImportFromPem(stored.PublicKeyPem); return configured;
        }
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("eMilimo.API.Licensing.bau-license-public.pem") ?? throw new CryptographicException("License verification key is unavailable.");
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
    private static LicenseState State(bool installed, bool authentic, bool valid, bool perpetual, bool trial, string status, string customer, string licensedSystem, string expectedIdentifier, string expectedSystem, string expiry, DateTime? end, string email, string phone, TimeSpan? remaining = null) =>
        new(installed, authentic, valid, perpetual, trial, status, customer, licensedSystem, expectedIdentifier, expectedSystem, expiry, end,
            perpetual ? null : Math.Max(0, (int)Math.Ceiling((remaining ?? TimeSpan.Zero).TotalDays)), perpetual ? null : Math.Max(0, (long)(remaining ?? TimeSpan.Zero).TotalSeconds), email, phone);
    private Task<string?> Value(string key) => db.SystemConfigs.AsNoTracking().Where(x => !x.IsArchived && x.ConfigKey.Trim() == key).Select(x => x.ConfigValue).FirstOrDefaultAsync();
    private async Task<string?> ReadPayloadAsync()
    {
        var protectedPayload = await db.SystemLicenses.AsNoTracking().OrderByDescending(x => x.InstalledAtUtc).Select(x => x.ProtectedPayload).FirstOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(protectedPayload)) { try { return _protector.Unprotect(protectedPayload); } catch { return "<invalid-protected-license/>"; } }
        return null;
    }
}
