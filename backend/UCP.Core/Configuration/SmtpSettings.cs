namespace UCP.Core.Configuration;

/// <summary>
/// SMTP configuration, bound from the "Smtp" section of appsettings.json /
/// appsettings.{Environment}.json, and overridable via environment variables
/// (e.g. Smtp__Password, Smtp__Username) or user-secrets in development.
///
/// Never commit real SMTP credentials — Username/Password should come from
/// environment variables or a secrets store in every non-local environment.
/// </summary>
public class SmtpSettings
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "UCZ Trinity Congregation";

    /// <summary>
    /// Timeout in milliseconds for the SMTP client.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 15000;
}