using System;
using System.Collections.Generic;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    public class Section : BaseEntity
    {
        public int SectionNumber { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<User>? Users { get; set; }
    }
}