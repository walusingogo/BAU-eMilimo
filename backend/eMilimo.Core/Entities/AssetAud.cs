using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eMilimo.Core.Entities;
[Table("asset_aud")]
public class AssetAud
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid? AssetCheckoutId { get; set; }
    [MaxLength(30)] public string RecordType { get; set; } = "ASSET";
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; }
}
