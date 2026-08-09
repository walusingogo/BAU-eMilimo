using System.ComponentModel.DataAnnotations;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities;
public class AssetCheckout : BaseEntity
{
    [MaxLength(40)] public string RequestNumber { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public Guid RequestedById { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? ApprovedById { get; set; }
    public int Quantity { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "PENDING";
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpectedOutAtUtc { get; set; }
    public DateTime ExpectedReturnAtUtc { get; set; }
    public DateTime? CheckedOutAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public string? ApprovalNote { get; set; }
    public bool PaymentRequired { get; set; }
    public decimal? PaymentAmount { get; set; }
    public bool? PaymentHonored { get; set; }
    public DateTime? PaymentHonoredAtUtc { get; set; }
    public Guid? PaymentVerifiedById { get; set; }
    public Asset? Asset { get; set; }
}
