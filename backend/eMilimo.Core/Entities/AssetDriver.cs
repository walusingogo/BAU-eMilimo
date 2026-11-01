using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

[Table("asset_drivers")]
public class AssetDriver : BaseEntity
{
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(200), EmailAddress] public string Email { get; set; } = string.Empty;
    [MaxLength(30), Phone] public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
