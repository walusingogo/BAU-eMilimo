namespace UCP.Core.Entities;
public class AssetMovement
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid? CheckoutId { get; set; }
    public int QuantityChange { get; set; }
    public int BalanceAfter { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; }
    public string? Notes { get; set; }
}
