using eMilimo.Core.Entities;

namespace eMilimo.Core.Services;

public interface IMembershipDirectoryProvider
{
    string ProviderCode { get; }
    Task<IReadOnlyList<MemberDirectoryResult>> SearchAsync(string query, int limit, CancellationToken ct = default);
    Task<MemberDirectoryResult?> GetAsync(string externalId, CancellationToken ct = default);
    Task<MembershipProviderHealth> TestAsync(CancellationToken ct = default);
}

public sealed record MemberDirectoryResult(string ExternalId, string MembershipNumber, string DisplayName, string? PhoneNumber, string? Email, string? ExternalVersion);
public sealed record MembershipProviderHealth(bool Available, string Message, DateTime CheckedAtUtc);
