// backend/eMilimo.Core/Entities/ContributionSource.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    [Table("contribution_sources")]
    public class ContributionSource : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
    }
}