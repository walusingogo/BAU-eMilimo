namespace eMilimo.Core.Configuration;

public sealed class UploadSettings
{
    public long MaximumFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".pdf", ".jpg", ".jpeg", ".png"];
    public string[] AllowedContentTypes { get; set; } = ["application/pdf", "image/jpeg", "image/png"];
}
