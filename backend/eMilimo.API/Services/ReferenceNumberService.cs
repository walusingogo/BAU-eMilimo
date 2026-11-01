using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Services;

/// <summary>
/// Allocates a global daily number per reference type with a single PostgreSQL
/// upsert. The row-level update lock makes allocation safe across users,
/// sections, processes and clustered application instances.
/// </summary>
public sealed class ReferenceNumberService(AppDbContext db, IConfiguration configuration) : IReferenceNumberService
{
    private static readonly IReadOnlyDictionary<string, string> DefaultPrefixes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CONTRIBUTION"] = "CON", ["FRB"] = "FRB", ["REQUISITION"] = "REQ", ["ASSET"] = "AST", ["SERVICE_COLLECTION"] = "SCL",
            ["DIGITAL_GIVING"] = "DGP"
        };

    public async Task<string> NextAsync(string referenceType, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var normalized = referenceType.Trim().ToUpperInvariant();
        if (!DefaultPrefixes.ContainsKey(normalized))
            throw new ArgumentOutOfRangeException(nameof(referenceType), "Unknown reference-number type.");

        var allocationDate = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ResolveTimeZone()));
        var configuration = await db.ReferenceNumberConfigurations.AsNoTracking()
            .Where(x => x.ReferenceType == normalized && x.EffectiveFrom <= allocationDate)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new { x.Prefix, x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);
        if (configuration is { IsActive: false })
            throw new InvalidOperationException($"Reference numbering for {normalized} is inactive.");
        var prefix = configuration?.Prefix ?? DefaultPrefixes[normalized];

        ValidatePrefix(prefix);
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO reference_number_sequences (reference_type, sequence_date, last_value, updated_at_utc)
            VALUES (@type, @date, 1, now())
            ON CONFLICT (reference_type, sequence_date)
            DO UPDATE SET last_value = reference_number_sequences.last_value + 1, updated_at_utc = now()
            RETURNING last_value;
            """;
        Add(command, "type", normalized);
        Add(command, "date", allocationDate);
        if (db.Database.CurrentTransaction != null)
            command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        var value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        return Format(prefix, value, allocationDate);
    }

    public static string Format(string prefix, long sequence, DateOnly date)
    {
        ValidatePrefix(prefix);
        if (sequence < 1) throw new ArgumentOutOfRangeException(nameof(sequence));
        return $"{prefix.ToUpperInvariant()}{sequence.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)}{date:yyyyMMdd}";
    }

    private static void ValidatePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > 10 || prefix.Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new InvalidOperationException("Reference prefixes must contain 1-10 ASCII letters or digits.");
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }

    private TimeZoneInfo ResolveTimeZone()
    {
        var id = configuration["Platform:TimeZone"] ?? "UTC";
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { throw new InvalidOperationException($"Configured platform time zone '{id}' is not installed on this host."); }
        catch (InvalidTimeZoneException) { throw new InvalidOperationException($"Configured platform time zone '{id}' is invalid."); }
    }
}
