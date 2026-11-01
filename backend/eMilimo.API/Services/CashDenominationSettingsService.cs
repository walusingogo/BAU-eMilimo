using System.Globalization;
using eMilimo.Core;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

public interface ICashDenominationSettingsService
{
    Task<IReadOnlyList<decimal>> GetAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<decimal>> SaveAsync(IEnumerable<decimal> values, Guid actorId, CancellationToken cancellationToken = default);
}

public sealed class CashDenominationSettingsService(AppDbContext db) : ICashDenominationSettingsService
{
    public const string ConfigKey = "finance.cash_denominations";

    public async Task<IReadOnlyList<decimal>> GetAsync(CancellationToken cancellationToken = default)
    {
        var stored = await db.SystemConfigs.AsNoTracking()
            .Where(x => x.ConfigKey == ConfigKey && !x.IsArchived)
            .Select(x => x.ConfigValue)
            .SingleOrDefaultAsync(cancellationToken);

        return Parse(stored) ?? FinancialDenominations.BaseCurrency;
    }

    public async Task<IReadOnlyList<decimal>> SaveAsync(IEnumerable<decimal> values, Guid actorId, CancellationToken cancellationToken = default)
    {
        var normalized = values
            .Select(x => decimal.Round(x, 2, MidpointRounding.AwayFromZero))
            .Distinct()
            .OrderByDescending(x => x)
            .ToArray();

        if (normalized.Length == 0) throw new InvalidOperationException("Add at least one cash denomination.");
        if (normalized.Any(x => x <= 0 || x > 1_000_000m))
            throw new InvalidOperationException("Cash denominations must be greater than zero and no more than 1,000,000.");

        var serialized = string.Join(",", normalized.Select(x => x.ToString("0.00", CultureInfo.InvariantCulture)));
        var config = await db.SystemConfigs.SingleOrDefaultAsync(x => x.ConfigKey == ConfigKey, cancellationToken);
        if (config == null)
        {
            config = new SystemConfig
            {
                Id = Guid.NewGuid(),
                ConfigKey = ConfigKey,
                ConfigValue = serialized,
                Description = "Base-currency cash notes and coins available on financial capture screens.",
                CreatedById = actorId,
                UpdatedById = actorId
            };
            db.SystemConfigs.Add(config);
        }
        else
        {
            config.ConfigValue = serialized;
            config.IsArchived = false;
            config.UpdatedById = actorId;
            config.UpdatedDate = DateTime.UtcNow;
            config.Version++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    private static decimal[]? Parse(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        var values = new List<decimal>();
        foreach (var part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!decimal.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value <= 0)
                return null;
            values.Add(decimal.Round(value, 2, MidpointRounding.AwayFromZero));
        }
        return values.Distinct().OrderByDescending(x => x).ToArray();
    }
}
