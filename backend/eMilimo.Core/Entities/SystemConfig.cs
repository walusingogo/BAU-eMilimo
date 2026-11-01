using System;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    public class SystemConfig : BaseEntity
    {
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsArchived { get; set; } = false;
    }
}