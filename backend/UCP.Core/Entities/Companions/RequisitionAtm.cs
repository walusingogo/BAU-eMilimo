using System;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities.Companions
{
    public class RequisitionAtm : AttachmentCompanionBase
    {
        public Requisition? Requisition { get; set; }
    }
}