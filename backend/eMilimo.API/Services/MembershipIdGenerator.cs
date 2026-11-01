using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

public interface IMembershipIdGenerator { Task<string> GenerateAsync(string role, CancellationToken cancellationToken = default); }

public sealed class MembershipIdGenerator(AppDbContext db, IConfiguration configuration) : IMembershipIdGenerator
{
    public async Task<string> GenerateAsync(string role, CancellationToken cancellationToken = default)
    {
        var pattern = configuration[$"Identity:MembershipIdPatterns:{role}"]
            ?? configuration["Identity:MembershipIdPatterns:Default"] ?? "USR-{0000}";
        var token = Regex.Match(pattern, @"\{(0+)\}");
        if (!token.Success) throw new InvalidOperationException($"Membership ID pattern for {role} must contain a numeric token such as {{0000}}.");
        var prefix = pattern[..token.Index]; var suffix = pattern[(token.Index + token.Length)..];
        var candidates = await db.Users.AsNoTracking().Where(x => x.MembershipId.StartsWith(prefix) && x.MembershipId.EndsWith(suffix))
            .Select(x => x.MembershipId).ToListAsync(cancellationToken);
        var next = candidates.Select(x => x.Length >= prefix.Length + suffix.Length &&
            int.TryParse(x.Substring(prefix.Length, x.Length - prefix.Length - suffix.Length), out var value) ? value : 0).DefaultIfEmpty().Max() + 1;
        return $"{prefix}{next.ToString(new string('0', token.Groups[1].Value.Length))}{suffix}";
    }
}
