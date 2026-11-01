using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eMilimo.Core.Entities.Companions;

[Table("asset_checkouts_atms")]
public class AssetCheckoutAtm : AttachmentCompanionBase
{
    [MaxLength(30)] public string AttachmentType { get; set; } = "DOCUMENT";
    public AssetCheckout? AssetCheckout { get; set; }
}
