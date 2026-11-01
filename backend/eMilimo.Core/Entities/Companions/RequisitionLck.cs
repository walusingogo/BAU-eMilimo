using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities.Companions
{
    public class RequisitionLck : LockCompanionBase
    {
        public Requisition? Requisition { get; set; }
    }
}