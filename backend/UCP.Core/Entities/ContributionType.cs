// backend/UCP.Core/Entities/ContributionType.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCP.Core.Entities.Base;

namespace UCP.Core.Entities
{
    [Table("contribution_types")]
    public class ContributionType : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    }
}