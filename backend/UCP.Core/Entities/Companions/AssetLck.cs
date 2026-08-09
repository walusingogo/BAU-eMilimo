using System.ComponentModel.DataAnnotations.Schema;

namespace UCP.Core.Entities.Companions;
[Table("asset_lcks")]
public class AssetLck : LockCompanionBase
{
    public Asset? Asset { get; set; }
}
