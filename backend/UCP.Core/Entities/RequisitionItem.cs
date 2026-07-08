using System;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    public class RequisitionItem : BaseEntity
    {
        public Guid RequisitionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }

        // Navigation
        public Requisition? Requisition { get; set; }
    }
}