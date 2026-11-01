using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

[Table("accounting_integration_profiles")]
public sealed class AccountingIntegrationProfile : BaseEntity
{
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    [MaxLength(50)] public string ProviderKey { get; set; } = "SAGE_PASTEL_PARTNER";
    [MaxLength(80)] public string ProductName { get; set; } = "Sage Pastel Partner";
    [MaxLength(40)] public string? ProductVersion { get; set; }
    [MaxLength(120)] public string CompanyIdentifier { get; set; } = string.Empty;
    [MaxLength(20)] public string IntegrationMode { get; set; } = "FILE";
    [MaxLength(3)] public string HomeCurrencyCode { get; set; } = "ZMW";
    public Guid? IntegrationSettingId { get; set; }
    public IntegrationSetting? IntegrationSetting { get; set; }
    public bool IsEnabled { get; set; }
    public ICollection<AccountingAccountMapping> Mappings { get; set; } = new List<AccountingAccountMapping>();
}

[Table("accounting_account_mappings")]
public sealed class AccountingAccountMapping : BaseEntity
{
    public Guid AccountingIntegrationProfileId { get; set; }
    public AccountingIntegrationProfile? AccountingIntegrationProfile { get; set; }
    [MaxLength(40)] public string MappingType { get; set; } = string.Empty;
    [MaxLength(160)] public string SourceKey { get; set; } = string.Empty;
    [MaxLength(120)] public string SourceLabel { get; set; } = string.Empty;
    [MaxLength(40)] public string? DebitAccountCode { get; set; }
    [MaxLength(40)] public string? CreditAccountCode { get; set; }
    [MaxLength(20)] public string? CashBookCode { get; set; }
    [MaxLength(20)] public string? EntryTypeCode { get; set; }
    [MaxLength(40)] public string? ProjectCode { get; set; }
    [MaxLength(20)] public string? TaxTypeCode { get; set; }
    public bool IsActive { get; set; } = true;
}

[Table("accounting_batches")]
public sealed class AccountingBatch : BaseEntity
{
    public Guid AccountingIntegrationProfileId { get; set; }
    public AccountingIntegrationProfile? AccountingIntegrationProfile { get; set; }
    [MaxLength(60)] public string BatchNumber { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "DRAFT";
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,2)")] public decimal DebitTotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CreditTotal { get; set; }
    [MaxLength(128)] public string SourceHash { get; set; } = string.Empty;
    [MaxLength(120)] public string? ExternalBatchReference { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? ExportedById { get; set; }
    public DateTime? ExportedAtUtc { get; set; }
    [MaxLength(1000)] public string? LastError { get; set; }
    public ICollection<AccountingBatchLine> Lines { get; set; } = new List<AccountingBatchLine>();
}

[Table("accounting_batch_lines")]
public sealed class AccountingBatchLine : BaseEntity
{
    public Guid AccountingBatchId { get; set; }
    public AccountingBatch? AccountingBatch { get; set; }
    public int LineNumber { get; set; }
    [MaxLength(40)] public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    [MaxLength(80)] public string SourceReference { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    [MaxLength(40)] public string AccountCode { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Debit { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Credit { get; set; }
    [MaxLength(255)] public string Description { get; set; } = string.Empty;
    [MaxLength(80)] public string Reference { get; set; } = string.Empty;
    [MaxLength(40)] public string? ProjectCode { get; set; }
    [MaxLength(20)] public string? TaxTypeCode { get; set; }
    [MaxLength(3)] public string CurrencyCode { get; set; } = "ZMW";
    [Column(TypeName = "decimal(18,2)")] public decimal OriginalCurrencyAmount { get; set; }
    [Column(TypeName = "decimal(28,12)")] public decimal ExchangeRate { get; set; } = 1m;
}
