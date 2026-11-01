using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;
public class Asset : BaseEntity
{
    [MaxLength(40)] public string AssetNumber { get; set; } = string.Empty;
    [MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(30)] public string Category { get; set; } = "INVENTORY";
    public Guid? AssetCategoryId { get; set; }
    public AssetCategory? AssetCategory { get; set; }
    public string? Description { get; set; }
    public int QuantityTotal { get; set; }
    public int QuantityAvailable { get; set; }
    [MaxLength(50)] public string? RegistrationNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
