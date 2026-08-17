// backend/UCP.Core/DTOs/SystemConfigDtos.cs
using System.ComponentModel.DataAnnotations;

namespace UCP.Core.DTOs
{
    public class SystemConfigDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsArchived { get; set; }
    }

    public class CreateSystemConfigDto
    {
        [Required(ErrorMessage = "ConfigKey is required.")]
        public string ConfigKey { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true, ErrorMessage = "ConfigValue is required.")]
        public string ConfigValue { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class UpdateSystemConfigDto
    {
        [Required(ErrorMessage = "ConfigKey is required.")]
        public string ConfigKey { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true, ErrorMessage = "ConfigValue is required.")]
        public string ConfigValue { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
