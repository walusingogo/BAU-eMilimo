using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    [Table("bank_accounts")]
    public class BankAccount : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string AccountName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Branch { get; set; }

        [MaxLength(50)]
        public string? AccountType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}