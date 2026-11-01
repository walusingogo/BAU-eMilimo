using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

public sealed class IntegrationSetting : BaseEntity
{
    public string IntegrationKey { get; set; } = "";
    public string ProtectedSettings { get; set; } = "";
}
