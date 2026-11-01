using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Entities.Companions;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Services;

public sealed record AccountingProfileInput(
    string Name,
    string ProviderKey,
    string ProductName,
    string? ProductVersion,
    string CompanyIdentifier,
    string IntegrationMode,
    string HomeCurrencyCode,
    Guid? IntegrationSettingId,
    bool IsEnabled,
    int? Version = null);

public sealed record AccountingMappingInput(
    Guid AccountingIntegrationProfileId,
    string MappingType,
    string SourceKey,
    string SourceLabel,
    string? DebitAccountCode,
    string? CreditAccountCode,
    string? CashBookCode,
    string? EntryTypeCode,
    string? ProjectCode,
    string? TaxTypeCode,
    bool IsActive,
    int? Version = null);

public sealed record PrepareAccountingBatchRequest(Guid ProfileId, DateOnly PeriodStart, DateOnly PeriodEnd);
public sealed record AccountingExportFile(string FileName, string ContentType, byte[] Content);
public sealed record AccountingCatalogOption(string Key, string Label);
public sealed record AccountingMappingCatalog(
    IReadOnlyList<AccountingCatalogOption> BankAccounts,
    IReadOnlyList<AccountingCatalogOption> PaymentMethods,
    IReadOnlyList<AccountingCatalogOption> ContributionTypes,
    IReadOnlyList<AccountingCatalogOption> Destinations);
public sealed record AccountingPreflightResult(int EligibleContributions, decimal TotalAmount, bool IsReady, IReadOnlyList<string> Issues);
public sealed record CancelAccountingBatchRequest(string Reason);

public interface IAccountingIntegrationService
{
    Task<IReadOnlyList<AccountingIntegrationProfile>> GetProfilesAsync(CancellationToken cancellationToken);
    Task<AccountingIntegrationProfile> SaveProfileAsync(Guid? id, AccountingProfileInput input, Guid actorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountingAccountMapping>> GetMappingsAsync(Guid profileId, CancellationToken cancellationToken);
    Task<AccountingAccountMapping> SaveMappingAsync(Guid? id, AccountingMappingInput input, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingMappingCatalog> GetMappingCatalogAsync(CancellationToken cancellationToken);
    Task<AccountingPreflightResult> PreflightAsync(PrepareAccountingBatchRequest request, CancellationToken cancellationToken);
    Task<AccountingBatch> PrepareBatchAsync(PrepareAccountingBatchRequest request, Guid actorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountingBatch>> GetBatchesAsync(Guid? profileId, string? status, CancellationToken cancellationToken);
    Task<AccountingBatch?> GetBatchAsync(Guid id, CancellationToken cancellationToken);
    Task<AccountingBatch> ApproveBatchAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingBatch> CancelBatchAsync(Guid id, string reason, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingExportFile> ExportBatchAsync(Guid id, Guid actorId, CancellationToken cancellationToken);
}

public interface IAccountingExportAdapter
{
    string ProviderKey { get; }
    AccountingExportFile Export(AccountingIntegrationProfile profile, AccountingBatch batch);
}

/// <summary>
/// Produces a conservative, documented CSV interchange file for Sage Pastel.
/// This adapter does not write to a Pastel database or assume an SDK version.
/// A licensed local connector can consume the same approved batch later.
/// </summary>
public sealed class SagePastelFileExportAdapter : IAccountingExportAdapter
{
    public string ProviderKey => "SAGE_PASTEL_PARTNER";

    public AccountingExportFile Export(AccountingIntegrationProfile profile, AccountingBatch batch)
    {
        var text = new StringBuilder();
        text.AppendLine("EntryType,CashBook,Date,Reference,Account,Description,Debit,Credit,Project,TaxType,Currency,OriginalAmount,ExchangeRate,SourceType,SourceId");
        foreach (var line in batch.Lines.OrderBy(x => x.LineNumber))
        {
            var mapping = profile.Mappings.FirstOrDefault(x =>
                x.IsActive &&
                ((line.Debit > 0 && string.Equals(x.DebitAccountCode, line.AccountCode, StringComparison.OrdinalIgnoreCase)) ||
                 (line.Credit > 0 && string.Equals(x.CreditAccountCode, line.AccountCode, StringComparison.OrdinalIgnoreCase))));
            text.AppendLine(string.Join(',', new[]
            {
                Csv(mapping?.EntryTypeCode), Csv(mapping?.CashBookCode), Csv(line.TransactionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(line.Reference), Csv(line.AccountCode), Csv(line.Description), Decimal(line.Debit), Decimal(line.Credit),
                Csv(line.ProjectCode), Csv(line.TaxTypeCode), Csv(line.CurrencyCode), Decimal(line.OriginalCurrencyAmount),
                line.ExchangeRate.ToString("0.############", CultureInfo.InvariantCulture), Csv(line.SourceType), Csv(line.SourceId.ToString("D"))
            }));
        }

        // UTF-8 BOM is deliberate: it opens cleanly in current Windows tools and
        // preserves congregation/member text. The import mapping remains explicit.
        var body = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
        return new AccountingExportFile($"{batch.BatchNumber}-sage-pastel.csv", "text/csv; charset=utf-8", body);
    }

    private static string Decimal(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Csv(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && "=+-@".Contains(safe[0])) safe = "'" + safe;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}

public sealed class AccountingIntegrationService(
    AppDbContext db,
    IEnumerable<IAccountingExportAdapter> adapters) : IAccountingIntegrationService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "DRAFT", "APPROVED", "EXPORTED", "CANCELLED" };

    public async Task<IReadOnlyList<AccountingIntegrationProfile>> GetProfilesAsync(CancellationToken cancellationToken) =>
        await db.AccountingIntegrationProfiles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<AccountingIntegrationProfile> SaveProfileAsync(Guid? id, AccountingProfileInput input, Guid actorId, CancellationToken cancellationToken)
    {
        ValidateProfile(input);
        AccountingIntegrationProfile entity;
        if (id.HasValue)
        {
            entity = await db.AccountingIntegrationProfiles.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
            EnsureVersion(entity.Version, input.Version);
            entity.UpdatedById = actorId;
            entity.UpdatedDate = DateTime.UtcNow;
            entity.Version++;
        }
        else
        {
            entity = new AccountingIntegrationProfile { Id = Guid.NewGuid(), CreatedById = actorId, UpdatedById = actorId };
            db.AccountingIntegrationProfiles.Add(entity);
        }

        entity.Name = input.Name.Trim();
        entity.ProviderKey = input.ProviderKey.Trim().ToUpperInvariant();
        entity.ProductName = input.ProductName.Trim();
        entity.ProductVersion = Clean(input.ProductVersion);
        entity.CompanyIdentifier = input.CompanyIdentifier.Trim();
        entity.IntegrationMode = input.IntegrationMode.Trim().ToUpperInvariant();
        entity.HomeCurrencyCode = input.HomeCurrencyCode.Trim().ToUpperInvariant();
        entity.IntegrationSettingId = input.IntegrationSettingId;
        entity.IsEnabled = input.IsEnabled;
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<AccountingAccountMapping>> GetMappingsAsync(Guid profileId, CancellationToken cancellationToken) =>
        await db.AccountingAccountMappings.AsNoTracking().Where(x => x.AccountingIntegrationProfileId == profileId)
            .OrderBy(x => x.MappingType).ThenBy(x => x.SourceLabel).ToListAsync(cancellationToken);

    public async Task<AccountingAccountMapping> SaveMappingAsync(Guid? id, AccountingMappingInput input, Guid actorId, CancellationToken cancellationToken)
    {
        ValidateMapping(input);
        if (!await db.AccountingIntegrationProfiles.AnyAsync(x => x.Id == input.AccountingIntegrationProfileId, cancellationToken))
            throw new KeyNotFoundException("Accounting integration profile was not found.");

        AccountingAccountMapping entity;
        if (id.HasValue)
        {
            entity = await db.AccountingAccountMappings.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Account mapping was not found.");
            EnsureVersion(entity.Version, input.Version);
            entity.UpdatedById = actorId;
            entity.UpdatedDate = DateTime.UtcNow;
            entity.Version++;
        }
        else
        {
            entity = new AccountingAccountMapping { Id = Guid.NewGuid(), CreatedById = actorId, UpdatedById = actorId };
            db.AccountingAccountMappings.Add(entity);
        }

        entity.AccountingIntegrationProfileId = input.AccountingIntegrationProfileId;
        entity.MappingType = input.MappingType.Trim().ToUpperInvariant();
        entity.SourceKey = input.SourceKey.Trim().ToUpperInvariant();
        entity.SourceLabel = input.SourceLabel.Trim();
        entity.DebitAccountCode = Clean(input.DebitAccountCode);
        entity.CreditAccountCode = Clean(input.CreditAccountCode);
        entity.CashBookCode = Clean(input.CashBookCode);
        entity.EntryTypeCode = Clean(input.EntryTypeCode);
        entity.ProjectCode = Clean(input.ProjectCode);
        entity.TaxTypeCode = Clean(input.TaxTypeCode);
        entity.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<AccountingMappingCatalog> GetMappingCatalogAsync(CancellationToken cancellationToken)
    {
        var bankAccounts = await db.BankAccounts.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.BankName).ThenBy(x => x.AccountName)
            .Select(x => new AccountingCatalogOption(x.Id.ToString().ToUpper(), x.BankName + " · " + x.AccountName + " · " + x.AccountNumber))
            .ToListAsync(cancellationToken);
        var paymentMethods = await db.PaymentMethodDefinitions.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new AccountingCatalogOption(x.Id.ToString().ToUpper(), x.Name + " (" + x.Code + ")"))
            .ToListAsync(cancellationToken);
        var contributionTypes = await db.ContributionTypes.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new AccountingCatalogOption(x.Id.ToString().ToUpper(), x.Name + " (" + x.Code + ")"))
            .ToListAsync(cancellationToken);
        var destinations = new List<AccountingCatalogOption> { new("ORGANIZATION", "Organization-wide") };
        destinations.AddRange(await db.Sections.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SectionNumber)
            .Select(x => new AccountingCatalogOption("SECTION:" + x.Id.ToString().ToUpper(), "Section · " + x.SectionName)).ToListAsync(cancellationToken));
        destinations.AddRange(await db.Groups.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.GroupName)
            .Select(x => new AccountingCatalogOption("GROUP:" + x.Id.ToString().ToUpper(), "Group · " + x.GroupName)).ToListAsync(cancellationToken));
        destinations.AddRange(await db.Committees.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.CommitteeName)
            .Select(x => new AccountingCatalogOption("COMMITTEE:" + x.Id.ToString().ToUpper(), "Committee · " + x.CommitteeName)).ToListAsync(cancellationToken));
        return new AccountingMappingCatalog(bankAccounts, paymentMethods, contributionTypes, destinations);
    }

    public async Task<AccountingPreflightResult> PreflightAsync(PrepareAccountingBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.PeriodEnd < request.PeriodStart) throw new ArgumentException("Period end must be on or after period start.");
        var profile = await db.AccountingIntegrationProfiles.AsNoTracking().Include(x => x.Mappings)
            .FirstOrDefaultAsync(x => x.Id == request.ProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        var contributions = await EligibleContributions(request, cancellationToken);
        var issues = new List<string>();
        if (!profile.IsEnabled) issues.Add("The selected accounting profile is disabled.");
        if (contributions.Count == 0) issues.Add("No newly posted contributions were found for the selected period.");
        var mappings = profile.Mappings.Where(x => x.IsActive).ToList();
        foreach (var contribution in contributions)
        {
            if (contribution.Allocations.Count == 0 || contribution.Allocations.Sum(x => x.AllocatedAmount) != contribution.Amount)
                issues.Add($"{contribution.ContributionNumber}: allocations are missing or do not equal the contribution total.");
            if (FindDebitMapping(mappings, contribution)?.DebitAccountCode is null)
                issues.Add($"{contribution.ContributionNumber}: no bank/payment-method debit mapping exists.");
            foreach (var typeId in contribution.Allocations.Select(x => x.ContributionTypeId ?? contribution.ContributionTypeId).Distinct())
                if (!mappings.Any(x => x.MappingType == "CONTRIBUTION_TYPE" && x.SourceKey == typeId.ToString("D").ToUpperInvariant() && !string.IsNullOrWhiteSpace(x.CreditAccountCode)))
                    issues.Add($"{contribution.ContributionNumber}: contribution type {typeId:D} has no credit mapping.");
        }
        var distinct = issues.Distinct().Take(100).ToList();
        return new AccountingPreflightResult(contributions.Count, contributions.Sum(x => x.Amount), distinct.Count == 0, distinct);
    }

    public async Task<AccountingBatch> PrepareBatchAsync(PrepareAccountingBatchRequest request, Guid actorId, CancellationToken cancellationToken)
    {
        if (request.PeriodEnd < request.PeriodStart) throw new ArgumentException("Period end must be on or after period start.");
        var profile = await db.AccountingIntegrationProfiles.Include(x => x.Mappings)
            .FirstOrDefaultAsync(x => x.Id == request.ProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        if (!profile.IsEnabled) throw new InvalidOperationException("The accounting integration profile is disabled.");

        var contributions = await EligibleContributions(request, cancellationToken);
        if (contributions.Count == 0) throw new InvalidOperationException("No newly posted contributions were found for the selected period.");

        var mappings = profile.Mappings.Where(x => x.IsActive).ToList();
        var lines = new List<AccountingBatchLine>();
        var errors = new List<string>();
        var number = 1;
        foreach (var contribution in contributions)
        {
            if (contribution.Allocations.Count == 0 || contribution.Allocations.Sum(x => x.AllocatedAmount) != contribution.Amount)
            {
                errors.Add($"{contribution.ContributionNumber}: allocations are missing or do not equal the contribution total.");
                continue;
            }

            var debitMapping = FindDebitMapping(mappings, contribution);
            if (debitMapping?.DebitAccountCode is null)
            {
                errors.Add($"{contribution.ContributionNumber}: no active bank/payment-method debit mapping exists.");
                continue;
            }

            lines.Add(NewLine(contribution, number++, debitMapping.DebitAccountCode, contribution.Amount, 0m, debitMapping.ProjectCode));
            foreach (var allocation in contribution.Allocations.OrderBy(x => x.Id))
            {
                var typeId = allocation.ContributionTypeId ?? contribution.ContributionTypeId;
                var creditMapping = mappings.FirstOrDefault(x => x.MappingType == "CONTRIBUTION_TYPE" && x.SourceKey == typeId.ToString("D").ToUpperInvariant());
                if (creditMapping?.CreditAccountCode is null)
                {
                    errors.Add($"{contribution.ContributionNumber}: contribution type {typeId:D} has no active credit mapping.");
                    continue;
                }

                var destinationKey = allocation.SectionId.HasValue ? $"SECTION:{allocation.SectionId:D}"
                    : allocation.GroupId.HasValue ? $"GROUP:{allocation.GroupId:D}"
                    : allocation.CommitteeId.HasValue ? $"COMMITTEE:{allocation.CommitteeId:D}" : "ORGANIZATION";
                var destination = mappings.FirstOrDefault(x => x.MappingType == "DESTINATION" && x.SourceKey == destinationKey.ToUpperInvariant());
                lines.Add(NewLine(contribution, number++, creditMapping.CreditAccountCode, 0m, allocation.AllocatedAmount,
                    destination?.ProjectCode ?? creditMapping.ProjectCode));
            }
        }

        if (errors.Count > 0) throw new InvalidOperationException("Accounting mappings are incomplete. " + string.Join(" ", errors.Distinct().Take(20)));
        var debitTotal = lines.Sum(x => x.Debit);
        var creditTotal = lines.Sum(x => x.Credit);
        if (debitTotal <= 0 || debitTotal != creditTotal) throw new InvalidOperationException("The generated accounting batch is not balanced.");

        var now = DateTime.UtcNow;
        var batch = new AccountingBatch
        {
            Id = Guid.NewGuid(), AccountingIntegrationProfileId = profile.Id,
            BatchNumber = $"ACC-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            PeriodStart = request.PeriodStart, PeriodEnd = request.PeriodEnd, Status = "DRAFT",
            CurrencyCode = profile.HomeCurrencyCode, DebitTotal = debitTotal, CreditTotal = creditTotal,
            SourceHash = SourceHash(profile.Id, contributions), CreatedById = actorId, UpdatedById = actorId,
            CreatedDate = now, UpdatedDate = now, Lines = lines
        };
        db.AccountingBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batch.Id, cancellationToken) ?? batch;
    }

    public async Task<IReadOnlyList<AccountingBatch>> GetBatchesAsync(Guid? profileId, string? status, CancellationToken cancellationToken)
    {
        var query = db.AccountingBatches.AsNoTracking().Include(x => x.AccountingIntegrationProfile).AsQueryable();
        if (profileId.HasValue) query = query.Where(x => x.AccountingIntegrationProfileId == profileId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = status.Trim().ToUpperInvariant();
            if (!AllowedStatuses.Contains(normalized)) throw new ArgumentException("Invalid accounting batch status.");
            query = query.Where(x => x.Status == normalized);
        }
        return await query.OrderByDescending(x => x.CreatedDate).Take(500).ToListAsync(cancellationToken);
    }

    public Task<AccountingBatch?> GetBatchAsync(Guid id, CancellationToken cancellationToken) =>
        db.AccountingBatches.AsNoTracking().Include(x => x.AccountingIntegrationProfile).ThenInclude(x => x!.Mappings)
            .Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<AccountingBatch> ApproveBatchAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var batch = await db.AccountingBatches.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting batch was not found.");
        if (batch.Status != "DRAFT") throw new InvalidOperationException("Only a draft accounting batch can be approved.");
        if (batch.CreatedById == actorId) throw new InvalidOperationException("The preparer cannot approve the same accounting batch.");
        if (batch.Lines.Count == 0 || batch.DebitTotal <= 0 || batch.DebitTotal != batch.CreditTotal ||
            batch.DebitTotal != batch.Lines.Sum(x => x.Debit) || batch.CreditTotal != batch.Lines.Sum(x => x.Credit))
            throw new InvalidOperationException("The accounting batch is empty or unbalanced.");
        batch.Status = "APPROVED";
        batch.ApprovedById = actorId;
        batch.ApprovedAtUtc = DateTime.UtcNow;
        batch.UpdatedById = actorId;
        batch.UpdatedDate = DateTime.UtcNow;
        batch.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batch.Id, cancellationToken) ?? batch;
    }

    public async Task<AccountingBatch> CancelBatchAsync(Guid id, string reason, Guid actorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new ArgumentException("A meaningful cancellation reason is required.");
        var batch = await db.AccountingBatches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting batch was not found.");
        if (batch.Status != "DRAFT") throw new InvalidOperationException("Only a draft accounting batch can be cancelled.");
        batch.Status = "CANCELLED";
        batch.LastError = reason.Trim();
        batch.UpdatedById = actorId;
        batch.UpdatedDate = DateTime.UtcNow;
        batch.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batch.Id, cancellationToken) ?? batch;
    }

    public async Task<AccountingExportFile> ExportBatchAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var batch = await db.AccountingBatches.Include(x => x.AccountingIntegrationProfile).ThenInclude(x => x!.Mappings)
            .Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting batch was not found.");
        if (batch.Status is not ("APPROVED" or "EXPORTED")) throw new InvalidOperationException("Only an approved accounting batch can be exported.");
        var profile = batch.AccountingIntegrationProfile ?? throw new InvalidOperationException("Accounting integration profile is unavailable.");
        var adapter = adapters.FirstOrDefault(x => string.Equals(x.ProviderKey, profile.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No safe export adapter is registered for {profile.ProviderKey}.");
        var file = adapter.Export(profile, batch);
        if (batch.Status != "EXPORTED")
        {
            batch.Status = "EXPORTED";
            batch.ExportedById = actorId;
            batch.ExportedAtUtc = DateTime.UtcNow;
            batch.ExternalBatchReference = Path.GetFileNameWithoutExtension(file.FileName);
            batch.UpdatedById = actorId;
            batch.UpdatedDate = DateTime.UtcNow;
            batch.Version++;
        }
        // Every generated/downloaded file is evidence-worthy, including a
        // repeat export of an already approved batch.
        db.AccountingBatchAudits.Add(new AccountingBatchAud
        {
            Id = Guid.NewGuid(), EntityId = batch.Id, Action = "FILE_GENERATED",
            ChangesJson = JsonSerializer.Serialize(new { file.FileName, file.ContentType, Size = file.Content.Length }),
            PerformedById = actorId, PerformedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return file;
    }

    private static AccountingAccountMapping? FindDebitMapping(IEnumerable<AccountingAccountMapping> mappings, Contribution contribution)
    {
        if (contribution.BankAccountId.HasValue)
        {
            var bank = mappings.FirstOrDefault(x => x.MappingType == "BANK_ACCOUNT" && x.SourceKey == contribution.BankAccountId.Value.ToString("D").ToUpperInvariant());
            if (bank is not null) return bank;
        }
        if (contribution.PaymentMethodDefinitionId.HasValue)
        {
            var definition = mappings.FirstOrDefault(x => x.MappingType == "PAYMENT_METHOD" && x.SourceKey == contribution.PaymentMethodDefinitionId.Value.ToString("D").ToUpperInvariant());
            if (definition is not null) return definition;
        }
        return mappings.FirstOrDefault(x => x.MappingType == "PAYMENT_METHOD" && x.SourceKey == contribution.PaymentMethod.Trim().ToUpperInvariant());
    }

    private async Task<List<Contribution>> EligibleContributions(PrepareAccountingBatchRequest request, CancellationToken cancellationToken)
    {
        var start = DateTime.SpecifyKind(request.PeriodStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endExclusive = DateTime.SpecifyKind(request.PeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var alreadyStaged = db.AccountingBatchLines
            .Where(x => x.SourceType == "CONTRIBUTION" && x.AccountingBatch!.Status != "CANCELLED")
            .Select(x => x.SourceId);
        return await db.Contributions.AsNoTracking()
            .Include(x => x.Allocations).Include(x => x.ContributionType)
            .Where(x => x.Status == "CONFIRMED" && !x.IsArchived && x.ContributionDate >= start && x.ContributionDate < endExclusive)
            .Where(x => !alreadyStaged.Contains(x.Id))
            .Where(x => db.FundTransactions.Any(t => t.ReferenceType == "CONTRIBUTION" && t.ReferenceId == x.Id && !t.IsReversed))
            .OrderBy(x => x.ContributionDate).ThenBy(x => x.ContributionNumber)
            .ToListAsync(cancellationToken);
    }

    private static AccountingBatchLine NewLine(Contribution contribution, int number, string account, decimal debit, decimal credit, string? project) => new()
    {
        Id = Guid.NewGuid(), LineNumber = number, SourceType = "CONTRIBUTION", SourceId = contribution.Id,
        SourceReference = contribution.ContributionNumber, TransactionDate = DateOnly.FromDateTime(contribution.ContributionDate),
        AccountCode = account, Debit = debit, Credit = credit,
        Description = $"Contribution {contribution.ContributionNumber} - {contribution.ContributionType?.Name ?? "Contribution"}",
        Reference = contribution.ReferenceNumber ?? contribution.ContributionNumber, ProjectCode = project,
        CurrencyCode = contribution.CurrencyCode, OriginalCurrencyAmount = debit > 0 ? contribution.OriginalCurrencyAmount : credit,
        ExchangeRate = contribution.ExchangeRateSnapshot <= 0 ? 1m : contribution.ExchangeRateSnapshot,
        CreatedById = contribution.CreatedById, UpdatedById = contribution.UpdatedById
    };

    private static string SourceHash(Guid profileId, IEnumerable<Contribution> contributions)
    {
        var source = profileId.ToString("N") + "|" + string.Join('|', contributions.OrderBy(x => x.Id)
            .Select(x => $"{x.Id:N}:{x.Amount:0.00}:{x.Version}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static void ValidateProfile(AccountingProfileInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.ProviderKey) ||
            string.IsNullOrWhiteSpace(input.ProductName) || string.IsNullOrWhiteSpace(input.CompanyIdentifier))
            throw new ArgumentException("Name, provider, product and company identifier are required.");
        if (input.HomeCurrencyCode.Trim().Length != 3) throw new ArgumentException("Home currency must be a three-letter ISO code.");
        if (!string.Equals(input.IntegrationMode.Trim(), "FILE", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(input.IntegrationMode.Trim(), "CONNECTOR", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Integration mode must be FILE or CONNECTOR.");
    }

    private static void ValidateMapping(AccountingMappingInput input)
    {
        var type = input.MappingType.Trim().ToUpperInvariant();
        if (type is not ("BANK_ACCOUNT" or "PAYMENT_METHOD" or "CONTRIBUTION_TYPE" or "DESTINATION"))
            throw new ArgumentException("Mapping type must be BANK_ACCOUNT, PAYMENT_METHOD, CONTRIBUTION_TYPE or DESTINATION.");
        if (string.IsNullOrWhiteSpace(input.SourceKey) || string.IsNullOrWhiteSpace(input.SourceLabel))
            throw new ArgumentException("Mapping source and label are required.");
        if (type is "BANK_ACCOUNT" or "PAYMENT_METHOD" && string.IsNullOrWhiteSpace(input.DebitAccountCode))
            throw new ArgumentException("Receipt mappings require a debit account.");
        if (type == "CONTRIBUTION_TYPE" && string.IsNullOrWhiteSpace(input.CreditAccountCode))
            throw new ArgumentException("Contribution type mappings require a credit account.");
        if (type == "DESTINATION" && string.IsNullOrWhiteSpace(input.ProjectCode))
            throw new ArgumentException("Destination mappings require a project code.");
    }

    private static void EnsureVersion(int current, int? supplied)
    {
        if (!supplied.HasValue || supplied.Value != current)
            throw new DbUpdateConcurrencyException("This record changed while you were editing it. Reload and try again.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
