using System.ComponentModel.DataAnnotations;
using eMilimo.Core.Entities.Base;

namespace eMilimo.Core.Entities
{
    /// <summary>
    /// Singleton settings row (there is only ever one) holding the SMTP
    /// configuration editable from System Settings. This is the source of
    /// truth for outbound email at runtime; appsettings.json's Smtp:* section
    /// is only used as the bootstrap default the first time the application
    /// starts against a fresh database (see EmailService/ISmtpSettingsProvider).
    ///
    /// Password is stored as configured by an administrator via the Settings
    /// UI — it is never returned by the GET endpoint (write-only field from
    /// the API's perspective) and is never written to logs.
    /// </summary>
    public class SmtpSetting : BaseEntity
    {
        public bool Enabled { get; set; } = false;

        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;

        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(255)]
        public string FromEmail { get; set; } = string.Empty;

        [MaxLength(255)]
        public string FromName { get; set; } = "UCZ Trinity Congregation";

        public int TimeoutMilliseconds { get; set; } = 15000;
    }
}