using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using eMilimo.Core.Enums;

namespace eMilimo.Core.DTOs
{
    public class RequisitionStatsDto
    {
        public int Total { get; set; }
        public int Draft { get; set; }
        public int Pending { get; set; }
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

    // ============ REQUISITION ITEM ============
    public class RequisitionItemDto
    {
        public string Description { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    // ============ CREATE REQUISITION ============
    public class CreateRequisitionDto
    {
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }
        public string Payee { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; }
        public FundSource FundSource { get; set; }
        public string PaymentDetails { get; set; } = null!;
        public List<RequisitionItemDto> Items { get; set; } = new();
        public List<IFormFile>? Files { get; set; }
    }

    // ============ UPDATE REQUISITION ============
    public class UpdateRequisitionDto
    {
        public string? Payee { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? PaymentDetails { get; set; }
        public decimal? Amount { get; set; }
        public int? Version { get; set; }
        public List<RequisitionItemDto>? Items { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    // ============ APPROVAL DTOS ============
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

    // ============ FILTER ============
    public class RequisitionFilterDto
    {
        public string? Status { get; set; }
        public Guid? GroupId { get; set; }
        public Guid? SectionId { get; set; }
        public Guid? CommitteeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    // ============ ATTACHMENT RESPONSE DTO ============
    public class RequisitionAttachmentResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public Guid? UploadedBy { get; set; }
    }
}
