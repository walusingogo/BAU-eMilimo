using UCP.Core.Entities.Base;

namespace UCP.Core.Entities;

public sealed class LicenseTrustedKey : BaseEntity
{
    public string ProductCode { get; set; } = "UCP";
    public string KeyId { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public long RotationSequence { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? InstalledById { get; set; }
    public string RotationPackageHash { get; set; } = string.Empty;
}
