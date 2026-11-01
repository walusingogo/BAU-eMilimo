using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

/// <summary>
/// Stores the currently installed license outside generic system settings.
/// The payload is protected by ASP.NET Core Data Protection before it reaches
/// the database, so the license key is neither readable nor safely editable.
/// </summary>
public sealed class SystemLicense : BaseEntity
{
    public string ProtectedPayload { get; set; } = string.Empty;
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? InstalledById { get; set; }
}
