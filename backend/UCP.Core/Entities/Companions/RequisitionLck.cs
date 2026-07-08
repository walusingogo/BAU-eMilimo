using UCP.Core.Entities.Base;

namespace UCP.Core.Entities.Companions
{
    public class RequisitionLck : LockCompanionBase
    {
        public Requisition? Requisition { get; set; }
    }
}