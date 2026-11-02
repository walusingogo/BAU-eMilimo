using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eMilimo.Core.Entities.Companions;

public abstract class AccountingAuditCompanionBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EntityId { get; set; }
    [MaxLength(60)] public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
    public Guid? PerformedById { get; set; }
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string CorrelationId { get; set; } = string.Empty;
}

[Table("accounting_integration_profile_atm")] public sealed class AccountingIntegrationProfileAtm : AttachmentCompanionBase { }
[Table("accounting_integration_profile_lck")] public sealed class AccountingIntegrationProfileLck : LockCompanionBase { }
[Table("accounting_integration_profile_aud")] public sealed class AccountingIntegrationProfileAud : AccountingAuditCompanionBase { }
[Table("accounting_account_mapping_atm")] public sealed class AccountingAccountMappingAtm : AttachmentCompanionBase { }
[Table("accounting_account_mapping_lck")] public sealed class AccountingAccountMappingLck : LockCompanionBase { }
[Table("accounting_account_mapping_aud")] public sealed class AccountingAccountMappingAud : AccountingAuditCompanionBase { }
[Table("accounting_external_code_atm")] public sealed class AccountingExternalCodeAtm : AttachmentCompanionBase { }
[Table("accounting_external_code_lck")] public sealed class AccountingExternalCodeLck : LockCompanionBase { }
[Table("accounting_external_code_aud")] public sealed class AccountingExternalCodeAud : AccountingAuditCompanionBase { }
[Table("accounting_batch_atm")] public sealed class AccountingBatchAtm : AttachmentCompanionBase { }
[Table("accounting_batch_lck")] public sealed class AccountingBatchLck : LockCompanionBase { }
[Table("accounting_batch_aud")] public sealed class AccountingBatchAud : AccountingAuditCompanionBase { }
[Table("accounting_batch_line_atm")] public sealed class AccountingBatchLineAtm : AttachmentCompanionBase { }
[Table("accounting_batch_line_lck")] public sealed class AccountingBatchLineLck : LockCompanionBase { }
[Table("accounting_batch_line_aud")] public sealed class AccountingBatchLineAud : AccountingAuditCompanionBase { }
