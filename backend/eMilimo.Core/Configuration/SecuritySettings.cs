namespace eMilimo.Core.Configuration;

public class SecuritySettings
{
    public SessionSettings Session { get; set; } = new();
    public PasswordPolicySettings PasswordPolicy { get; set; } = new();
}

public class SessionSettings
{
    public int LifetimeMinutes { get; set; } = 480;
    public int IdleTimeoutMinutes { get; set; } = 30;
    public int WarningCountdownSeconds { get; set; } = 60;
}

public class PasswordPolicySettings
{
    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 128;
    public int ExpiryDays { get; set; } = 90;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
}
