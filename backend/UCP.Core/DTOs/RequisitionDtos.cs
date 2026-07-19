using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using UCP.Core.Enums;

namespace UCP.Core.DTOs
{

    public class RequisitionStatsDto
    {
        public int Total { get; set; }
        public int Draft { get; set; }
        public int Pending { get; set; } // SeniorRepsPending + ChurchAuthPending + MinisterPending
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int MoneyReady { get; set; }
        public int Collected { get; set; }
        public int Completed { get; set; }
        public int Archived { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
    }
    public class CreateRequisitionDto
    {
        public Guid GroupId { get; set; }
        public string Payee { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; }
        public FundSource FundSource { get; set; }
        public string PaymentDetails { get; set; } = null!;
        public List<CreateRequisitionItemDto> Items { get; set; } = new();
    }

    public class CreateRequisitionItemDto
    {
        public string Description { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class UpdateRequisitionDto
    {
        public string? Payee { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? PaymentDetails { get; set; }
        public List<CreateRequisitionItemDto>? Items { get; set; }
    }

    public class ApproveRequisitionDto
    {
        public string? SignatureData { get; set; }
        public string? Comments { get; set; }
    }

    public class RejectRequisitionDto
    {
        public string Reason { get; set; } = null!;
    }

    public class CollectRequisitionDto
    {
        public string? SignatureData { get; set; }
    }

    public class ArchiveRequisitionDto
    {
        public string? Reason { get; set; }
    }

    public class RequisitionFilterDto
    {
        public string? Status { get; set; }
        public Guid? GroupId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    // ============================================
    // NEW: Combined DTO for Requisition + Files
    // ============================================

    public class CreateRequisitionWithFilesDto
    {
        public Guid GroupId { get; set; }
        public string Payee { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; }
        public FundSource FundSource { get; set; }
        public string PaymentDetails { get; set; } = null!;
        public List<CreateRequisitionItemDto> Items { get; set; } = new();

        /// <summary>
        /// List of files to attach (invoices, receipts, etc.)
        /// </summary>
        public List<IFormFile>? Files { get; set; }
    }
}