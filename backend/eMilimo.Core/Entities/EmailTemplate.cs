using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities;

/// <summary>
/// Administrator-maintained content for a known transactional notification.
/// Template keys and allowed placeholders are defined by the application; only
/// the enabled flag, subject and HTML body are editable.
/// </summary>
public sealed class EmailTemplate : BaseEntity
{
    [Required, MaxLength(100)]
    public string TemplateKey { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    [Required, MaxLength(255)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    public bool IsSmsEnabled { get; set; } = true;

    [Required, MaxLength(1600)]
    public string SmsBody { get; set; } = string.Empty;

    public bool IsWhatsAppEnabled { get; set; } = true;

    [Required, MaxLength(4096)]
    public string WhatsAppBody { get; set; } = string.Empty;
}
