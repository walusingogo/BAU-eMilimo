using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public sealed record LicenseRotationSignature(string RootKeyId, string Signature);
public sealed record LicenseRotationPackage(int FormatVersion, string ProductCode, long Sequence, string NewKeyId,
    string NewPublicKeyPem, string NewPublicKeyFingerprint, DateTime IssuedAtUtc, DateTime ValidFromUtc,
    DateTime PackageExpiresAtUtc, string? PreviousKeyId, string PreviousKeyAction,
    IReadOnlyCollection<LicenseRotationSignature> Signatures);

public interface ILicenseKeyRotationService
{
    Task<object> GetStateAsync(CancellationToken cancellationToken = default);
    Task<object> InstallAsync(string packageJson, Guid userId, CancellationToken cancellationToken = default);
}

public sealed class LicenseKeyRotationService(AppDbContext db) : ILicenseKeyRotationService
{
    private const string Product = "eMilimo";
    private const int RequiredRecoverySignatures = 2;
    private static readonly string[] RecoveryKeyIds = ["bau-recovery-1", "bau-recovery-2", "bau-recovery-3"];

    public async Task<object> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var keys = await db.LicenseTrustedKeys.AsNoTracking().Where(x => x.ProductCode == Product)
            .OrderByDescending(x => x.RotationSequence).Select(x => new
            {
                x.KeyId, x.Fingerprint, x.Status, x.RotationSequence, x.ValidFromUtc, x.RevokedAtUtc, x.InstalledAtUtc
            }).ToListAsync(cancellationToken);
        var audits = await db.LicenseKeyRotationAudits.AsNoTracking().Where(x => x.ProductCode == Product)
            .OrderByDescending(x => x.UploadedAtUtc).Take(50).ToListAsync(cancellationToken);
        return new { ProductCode = Product, RequiredRecoverySignatures, RecoveryKeyCount = RecoveryKeyIds.Length, Keys = keys, Audits = audits };
    }

    public async Task<object> InstallAsync(string packageJson, Guid userId, CancellationToken cancellationToken = default)
    {
        var packageHash = Sha256(Encoding.UTF8.GetBytes(packageJson));
        LicenseRotationPackage? package = null;
        try
        {
            package = JsonSerializer.Deserialize<LicenseRotationPackage>(packageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (package is null) throw new InvalidDataException("The rotation package is empty.");
            ValidateStructure(package);

            if (await db.LicenseKeyRotationAudits.AnyAsync(x => x.PackageHash == packageHash && x.Result == "SUCCESS", cancellationToken))
                throw new InvalidOperationException("This rotation package has already been installed.");
            var lastSequence = await db.LicenseTrustedKeys.Where(x => x.ProductCode == Product)
                .Select(x => (long?)x.RotationSequence).MaxAsync(cancellationToken) ?? 0;
            if (package.Sequence <= lastSequence)
                throw new InvalidOperationException("The rotation package sequence is not newer than the installed sequence.");
            if (await db.LicenseTrustedKeys.AnyAsync(x => x.ProductCode == Product && x.KeyId == package.NewKeyId, cancellationToken))
                throw new InvalidOperationException("The proposed key identifier has already been used.");

            using var productKey = RSA.Create();
            productKey.ImportFromPem(package.NewPublicKeyPem);
            if (productKey.KeySize < 3072) throw new CryptographicException("The product signing key must be at least 3072 bits.");
            var publicDer = productKey.ExportSubjectPublicKeyInfo();
            var fingerprint = Sha256(publicDer);
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(fingerprint), Encoding.ASCII.GetBytes(package.NewPublicKeyFingerprint.ToUpperInvariant())))
                throw new CryptographicException("The public-key fingerprint does not match the package.");

            var canonical = Canonical(package, fingerprint);
            var validSigners = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in package.Signatures ?? [])
            {
                if (!RecoveryKeyIds.Contains(item.RootKeyId, StringComparer.Ordinal) || !validSigners.Add(item.RootKeyId)) continue;
                using var root = LoadRecoveryPublicKey(item.RootKeyId);
                byte[] signature;
                try { signature = Convert.FromBase64String(item.Signature); } catch { validSigners.Remove(item.RootKeyId); continue; }
                if (!root.VerifyData(Encoding.UTF8.GetBytes(canonical), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
                    validSigners.Remove(item.RootKeyId);
            }
            if (validSigners.Count < RequiredRecoverySignatures)
                throw new CryptographicException($"At least {RequiredRecoverySignatures} valid BAU recovery signatures are required.");

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(package.PreviousKeyId))
            {
                var previous = await db.LicenseTrustedKeys.FirstOrDefaultAsync(x => x.ProductCode == Product && x.KeyId == package.PreviousKeyId, cancellationToken);
                if (previous is null && package.PreviousKeyId == "bau-license-2026-01")
                {
                    var initialPem = ReadEmbeddedPublicKey("eMilimo.API.Licensing.bau-license-public.pem");
                    using var initialRsa = RSA.Create(); initialRsa.ImportFromPem(initialPem);
                    previous = new LicenseTrustedKey
                    {
                        Id = Guid.NewGuid(), ProductCode = Product, KeyId = package.PreviousKeyId,
                        PublicKeyPem = initialPem.Trim(), Fingerprint = Sha256(initialRsa.ExportSubjectPublicKeyInfo()),
                        Status = "ACTIVE", RotationSequence = 0, ValidFromUtc = DateTime.UnixEpoch,
                        InstalledAtUtc = DateTime.UtcNow, InstalledById = userId, RotationPackageHash = packageHash,
                        CreatedById = userId, UpdatedById = userId, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
                    };
                    db.LicenseTrustedKeys.Add(previous);
                }
                if (previous is not null && package.PreviousKeyAction == "REVOKE")
                {
                    previous.Status = "REVOKED";
                    previous.RevokedAtUtc = DateTime.UtcNow;
                    previous.UpdatedDate = DateTime.UtcNow;
                    previous.UpdatedById = userId;
                    previous.Version++;
                }
            }
            var key = new LicenseTrustedKey
            {
                Id = Guid.NewGuid(), ProductCode = Product, KeyId = package.NewKeyId, PublicKeyPem = package.NewPublicKeyPem.Trim(),
                Fingerprint = fingerprint, Status = package.ValidFromUtc > DateTime.UtcNow ? "SCHEDULED" : "ACTIVE",
                RotationSequence = package.Sequence, ValidFromUtc = package.ValidFromUtc, InstalledAtUtc = DateTime.UtcNow,
                InstalledById = userId, RotationPackageHash = packageHash, CreatedById = userId, UpdatedById = userId,
                CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, Version = 1
            };
            db.LicenseTrustedKeys.Add(key);
            db.LicenseKeyRotationAudits.Add(Audit(package, packageHash, userId, "SUCCESS", null));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new { Message = "The signed licence verification key was installed.", key.KeyId, key.Fingerprint, key.Status, key.RotationSequence };
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            db.LicenseKeyRotationAudits.Add(Audit(package, packageHash, userId, "FAILED", SafeReason(ex)));
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("The key-rotation package could not be authenticated or installed.");
        }
    }

    private static void ValidateStructure(LicenseRotationPackage package)
    {
        var now = DateTime.UtcNow;
        if (package.FormatVersion != 1 || package.ProductCode != Product) throw new InvalidDataException("Unsupported rotation package.");
        if (package.Sequence < 1 || string.IsNullOrWhiteSpace(package.NewKeyId) || package.NewKeyId.Length > 100) throw new InvalidDataException("Invalid key metadata.");
        if (!package.NewKeyId.All(x => char.IsLetterOrDigit(x) || x is '-' or '_')) throw new InvalidDataException("Invalid key identifier.");
        if (package.IssuedAtUtc.Kind != DateTimeKind.Utc || package.ValidFromUtc.Kind != DateTimeKind.Utc || package.PackageExpiresAtUtc.Kind != DateTimeKind.Utc) throw new InvalidDataException("Rotation dates must be UTC.");
        if (package.IssuedAtUtc > now.AddMinutes(5) || package.PackageExpiresAtUtc <= now || package.PackageExpiresAtUtc > package.IssuedAtUtc.AddDays(7)) throw new InvalidDataException("The rotation package is outside its validity window.");
        if (package.ValidFromUtc < package.IssuedAtUtc.AddMinutes(-5) || package.ValidFromUtc > package.IssuedAtUtc.AddDays(90)) throw new InvalidDataException("Invalid activation date.");
        if (package.PreviousKeyAction is not ("RETAIN" or "REVOKE")) throw new InvalidDataException("Invalid previous-key action.");
        if (package.NewPublicKeyPem.Length is < 400 or > 10000 || package.Signatures is null) throw new InvalidDataException("Invalid package content.");
    }

    public static string Canonical(LicenseRotationPackage p, string normalizedFingerprint) => string.Join('.', new[]
    {
        "BAU-KEY-ROTATION-V1", p.ProductCode, p.Sequence.ToString(CultureInfo.InvariantCulture), p.NewKeyId,
        normalizedFingerprint, p.IssuedAtUtc.ToString("O", CultureInfo.InvariantCulture),
        p.ValidFromUtc.ToString("O", CultureInfo.InvariantCulture), p.PackageExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture),
        p.PreviousKeyId ?? "", p.PreviousKeyAction
    }.Select(x => Convert.ToBase64String(Encoding.UTF8.GetBytes(x))));

    private static RSA LoadRecoveryPublicKey(string keyId)
    {
        var resource = $"eMilimo.API.Licensing.{keyId}.public.pem";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource) ?? throw new CryptographicException("A recovery verification key is unavailable.");
        using var reader = new StreamReader(stream);
        var rsa = RSA.Create(); rsa.ImportFromPem(reader.ReadToEnd()); return rsa;
    }
    private static string ReadEmbeddedPublicKey(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource) ?? throw new CryptographicException("An embedded verification key is unavailable.");
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    private static string Sha256(byte[] value) => Convert.ToHexString(SHA256.HashData(value));
    private static LicenseKeyRotationAud Audit(LicenseRotationPackage? p, string hash, Guid userId, string result, string? reason) => new()
    {
        Id = Guid.NewGuid(), ProductCode = p?.ProductCode ?? Product, Sequence = p?.Sequence ?? 0,
        PreviousKeyId = p?.PreviousKeyId, NewKeyId = p?.NewKeyId, Action = p?.PreviousKeyAction ?? "UNKNOWN",
        Result = result, FailureReason = reason, PackageHash = hash, UploadedById = userId, UploadedAtUtc = DateTime.UtcNow
    };
    private static string SafeReason(Exception ex)
    {
        var reason = ex.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return reason.Length > 500 ? reason[..500] : reason;
    }
}
