using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

public sealed class LocalMembershipDirectoryProvider(AppDbContext db) : IMembershipDirectoryProvider
{
    public string ProviderCode => "LOCAL";

    public async Task<IReadOnlyList<MemberDirectoryResult>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        var normalized = query.Trim().ToUpperInvariant();
        return await db.Members.AsNoTracking().Where(x => !x.IsArchived &&
                (x.MembershipNumber.ToUpper().Contains(normalized) || x.NormalizedSearchName.Contains(normalized) ||
                 (x.NormalizedPhoneNumber != null && x.NormalizedPhoneNumber.Contains(normalized))))
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Take(Math.Clamp(limit, 1, 100))
            .Select(x => new MemberDirectoryResult(x.Id.ToString(), x.MembershipNumber,
                x.PreferredName ?? (x.FirstName + " " + x.LastName), x.PhoneNumber, x.Email, x.Version.ToString()))
            .ToListAsync(ct);
    }

    public async Task<MemberDirectoryResult?> GetAsync(string externalId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(externalId, out var id)) return null;
        return await db.Members.AsNoTracking().Where(x => x.Id == id && !x.IsArchived)
            .Select(x => new MemberDirectoryResult(x.Id.ToString(), x.MembershipNumber,
                x.PreferredName ?? (x.FirstName + " " + x.LastName), x.PhoneNumber, x.Email, x.Version.ToString()))
            .FirstOrDefaultAsync(ct);
    }

    public Task<MembershipProviderHealth> TestAsync(CancellationToken ct = default) =>
        Task.FromResult(new MembershipProviderHealth(true, "The local membership register is available.", DateTime.UtcNow));
}
