using System.Security.Cryptography;
using System.Text;

namespace eMilimo.API.Payments;

internal static class DigitalGivingSecurity
{
    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public static string NewOpaqueToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static byte[] DeriveOtpHash(string code, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(code, salt, 120_000, HashAlgorithmName.SHA256, 32);

    public static bool FixedTimeEquals(string expectedHash, string value) =>
        FixedTimeEqualsBytes(
            Encoding.UTF8.GetBytes(expectedHash),
            Encoding.UTF8.GetBytes(Hash(value)));

    public static bool FixedTimeEqualsBytes(byte[] expected, byte[] actual) =>
        expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);

    public static string NormalizeLookup(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException("Membership number or phone number is required.");
        return trimmed.Any(char.IsDigit) && trimmed.All(c => char.IsDigit(c) || c is '+' or ' ' or '-' or '(' or ')')
            ? NormalizePhone(trimmed)
            : trimmed.ToUpperInvariant();
    }

    public static string NormalizePhone(string value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal))
            digits = digits[2..];
        if (digits.StartsWith('0'))
            digits = $"260{digits[1..]}";
        if (digits.Length < 10 || digits.Length > 15)
            throw new InvalidOperationException("Enter a valid mobile number including its country code.");
        return $"+{digits}";
    }

    public static string MaskPhone(string phone)
    {
        var normalized = NormalizePhone(phone);
        return normalized.Length <= 7 ? "****" : $"{normalized[..4]}***{normalized[^3..]}";
    }

    public static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0])) return "***";
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
