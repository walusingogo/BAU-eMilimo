using System;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities.Companions
{
    public class RequisitionAtm : AttachmentCompanionBase
    {
        public Requisition? Requisition { get; set; }
    }
}