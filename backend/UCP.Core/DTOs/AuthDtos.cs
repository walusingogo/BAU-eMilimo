namespace UCP.Core.DTOs
{
    public class LoginDto
    {
        public string? Identifier { get; set; }
        public string? Email { get; set; }
        public string Password { get; set; } = null!;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordDto
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    public class AdminSetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
