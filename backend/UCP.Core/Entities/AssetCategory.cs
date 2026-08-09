using System.ComponentModel.DataAnnotations;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities;
public class AssetCategory : BaseEntity
{
    [MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
