using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UCP.Core.Entities.Companions;
[Table("asset_atms")]
public class AssetAtm : AttachmentCompanionBase
{
    [MaxLength(30)] public string AttachmentType { get; set; } = "DOCUMENT";
    public Asset? Asset { get; set; }
}
