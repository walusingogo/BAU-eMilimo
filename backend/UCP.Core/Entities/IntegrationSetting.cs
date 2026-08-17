using UCP.Core.Entities.Base;

namespace UCP.Core.Entities;

public sealed class IntegrationSetting : BaseEntity
{
    public string IntegrationKey { get; set; } = "";
    public string ProtectedSettings { get; set; } = "";
}
