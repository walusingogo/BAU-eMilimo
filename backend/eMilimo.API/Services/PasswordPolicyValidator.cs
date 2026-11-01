using eMilimo.Core.Configuration;

namespace eMilimo.API.Services;

public static class PasswordPolicyValidator
{
    public static string? Validate(string? password, PasswordPolicySettings policy)
    {
        if (string.IsNullOrEmpty(password) || password.Length < policy.MinimumLength) return $"Password must be at least {policy.MinimumLength} characters.";
        if (policy.MaximumLength > 0 && password.Length > policy.MaximumLength) return $"Password cannot exceed {policy.MaximumLength} characters.";
        if (policy.RequireUppercase && !password.Any(char.IsUpper)) return "Password must contain an uppercase letter.";
        if (policy.RequireLowercase && !password.Any(char.IsLower)) return "Password must contain a lowercase letter.";
        if (policy.RequireDigit && !password.Any(char.IsDigit)) return "Password must contain a number.";
        if (policy.RequireSpecialCharacter && password.All(char.IsLetterOrDigit)) return "Password must contain a special character.";
        return null;
    }
}
