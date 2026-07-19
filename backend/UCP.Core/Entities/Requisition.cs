using System;
using System.Collections.Generic;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    public class Requisition : BaseEntity
    {
        public string RequisitionNumber { get; set; } = string.Empty;

        // Foreign Keys
        public Guid? GroupId { get; set; }
        public Guid? RequisitorId { get; set; }
        public Guid? ApprovedById { get; set; }
        public Guid? MoneyReadyById { get; set; }
        public Guid? CollectedById { get; set; }
        public Guid? ArchivedById { get; set; }

        // Details
        public string Payee { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Cheque, BankTransfer, MobileMoney
        public string FundSource { get; set; } = "Budgeted"; // Budgeted, OwnFunds
        public decimal Amount { get; set; }
        public string AmountWords { get; set; } = string.Empty;
        public string PaymentDetails { get; set; } = string.Empty;

        // Status
        public string Status { get; set; } = "Draft";
        public string? RejectionReason { get; set; }

        // Approval & Payment Tracking
        public DateTime? ApprovedAt { get; set; }
        public DateTime? MoneyReadyAt { get; set; }
        public DateTime? CollectedAt { get; set; }
        public string? CollectionSignature { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Archiving
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedAt { get; set; }
        public string? ArchiveReason { get; set; }

        // Navigation properties
        public Group? Group { get; set; }
        public User? Requisitor { get; set; }
        public User? ApprovedBy { get; set; }
        public User? MoneyReadyBy { get; set; }
        public User? CollectedBy { get; set; }
        public User? ArchivedBy { get; set; }

        // Collections
        public ICollection<RequisitionItem>? Items { get; set; }
    }
}