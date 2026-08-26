namespace UCP.Core.Entities;

public sealed class LicenseKeyRotationAud
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = "UCP";
    public long Sequence { get; set; }
    public string? PreviousKeyId { get; set; }
    public string? NewKeyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string PackageHash { get; set; } = string.Empty;
    public Guid? UploadedById { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
