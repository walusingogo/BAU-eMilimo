using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eMilimo.Core.Entities;
using eMilimo.Core.Entities.Companions;
using eMilimo.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
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
public sealed record AccountingExternalCodeInput(
    Guid AccountingIntegrationProfileId, string CodeType, string Code, string Name,
    string? Description, string? Category, bool AllowsDebit, bool AllowsCredit,
    bool IsActive, int? Version = null);
public sealed record AccountingCodeImportResult(int Added, int Updated, int Skipped, IReadOnlyList<string> Warnings);

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
public sealed record AccountingFileColumn(int Position, string Field, string Header, string? ConstantValue = null);
public sealed record AccountingFileLayout(
    string Delimiter, bool IncludeHeader, string DateFormat, string DecimalSeparator,
    string Encoding, string DebitCreditMode, IReadOnlyList<AccountingFileColumn> Columns,
    string? SampleFileName = null, string? SampleSha256 = null, DateTime? AnalysedAtUtc = null);
public sealed record AccountingFileLocations(
    string ExportPath, string ImportPath, string ArchivePath, bool CreateMissingDirectories,
    AccountingFileLayout? ExportLayout = null);
public sealed record AccountingLayoutAnalysis(
    AccountingFileLayout SuggestedLayout, IReadOnlyList<IReadOnlyList<string>> PreviewRows,
    IReadOnlyList<string> Warnings);
public sealed record AccountingLocationTestRequest(string LocationType, string? Path = null);
public sealed record AccountingLocationTestResult(string LocationType, string Path, bool Available, bool Writable, string Message);
public sealed record AccountingImportResultManifest(
    string BatchNumber, string Status, int AcceptedLines, int RejectedLines,
    string? Message, string? ExternalBatchReference);
public sealed record AccountingImportScanResult(
    int FilesFound, int Processed, int Skipped, int Failed, IReadOnlyList<string> Messages);
public sealed record AccountingOperationsSummary(
    int Draft, int Approved, int AwaitingResponse, int Reconciled, int Partial, int Rejected,
    DateTime? LastExportedAtUtc, DateTime? LastImportedAtUtc);

public interface IAccountingIntegrationService
{
    Task<IReadOnlyList<AccountingIntegrationProfile>> GetProfilesAsync(CancellationToken cancellationToken);
    Task<AccountingIntegrationProfile> SaveProfileAsync(Guid? id, AccountingProfileInput input, Guid actorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountingAccountMapping>> GetMappingsAsync(Guid profileId, CancellationToken cancellationToken);
    Task<AccountingAccountMapping> SaveMappingAsync(Guid? id, AccountingMappingInput input, Guid actorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountingExternalCode>> GetExternalCodesAsync(Guid profileId, string? codeType, bool includeInactive, CancellationToken cancellationToken);
    Task<AccountingExternalCode> SaveExternalCodeAsync(Guid? id, AccountingExternalCodeInput input, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingCodeImportResult> ImportExternalCodesAsync(Guid profileId, Stream source, string fileName, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingExportFile> ExportExternalCodesAsync(Guid profileId, CancellationToken cancellationToken);
    Task<AccountingMappingCatalog> GetMappingCatalogAsync(CancellationToken cancellationToken);
    Task<AccountingPreflightResult> PreflightAsync(PrepareAccountingBatchRequest request, CancellationToken cancellationToken);
    Task<AccountingFileLocations> GetFileLocationsAsync(Guid profileId, CancellationToken cancellationToken);
    Task<AccountingFileLocations> SaveFileLocationsAsync(Guid profileId, AccountingFileLocations locations, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingLocationTestResult> TestFileLocationAsync(Guid profileId, string locationType, string? candidatePath, CancellationToken cancellationToken);
    Task<AccountingFileLayout> GetFileLayoutAsync(Guid profileId, CancellationToken cancellationToken);
    Task<AccountingFileLayout> SaveFileLayoutAsync(Guid profileId, AccountingFileLayout layout, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingLayoutAnalysis> AnalyzeLayoutSampleAsync(Guid profileId, Stream sample, string fileName, Guid actorId, CancellationToken cancellationToken);
    Task<AccountingImportScanResult> ScanImportResponsesAsync(Guid profileId, Guid? actorId, CancellationToken cancellationToken);
    Task<AccountingOperationsSummary> GetOperationsSummaryAsync(Guid? profileId, CancellationToken cancellationToken);
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
    AccountingExportFile Export(AccountingIntegrationProfile profile, AccountingBatch batch, AccountingFileLayout? layout = null);
}

/// <summary>
/// Produces a conservative, documented CSV interchange file for Sage Pastel.
/// This adapter does not write to a Pastel database or assume an SDK version.
/// A licensed local connector can consume the same approved batch later.
/// </summary>
public sealed class SagePastelFileExportAdapter : IAccountingExportAdapter
{
    public string ProviderKey => "SAGE_PASTEL_PARTNER";

    public AccountingExportFile Export(AccountingIntegrationProfile profile, AccountingBatch batch, AccountingFileLayout? layout = null)
    {
        layout ??= AccountingIntegrationService.DefaultFileLayout();
        AccountingIntegrationService.ValidateFileLayout(layout);
        var delimiter = AccountingIntegrationService.DelimiterCharacter(layout.Delimiter);
        var text = new StringBuilder();
        var columns = layout.Columns.OrderBy(x => x.Position).ToList();
        if (layout.IncludeHeader) text.AppendLine(string.Join(delimiter, columns.Select(x => Csv(x.Header, delimiter))));
        foreach (var line in batch.Lines.OrderBy(x => x.LineNumber))
        {
            var mapping = profile.Mappings.FirstOrDefault(x =>
                x.IsActive &&
                ((line.Debit > 0 && string.Equals(x.DebitAccountCode, line.AccountCode, StringComparison.OrdinalIgnoreCase)) ||
                 (line.Credit > 0 && string.Equals(x.CreditAccountCode, line.AccountCode, StringComparison.OrdinalIgnoreCase))));
            text.AppendLine(string.Join(delimiter, columns.Select(column => Csv(Value(column, line, mapping, layout), delimiter))));
        }

        var encoding = ExportEncoding(layout.Encoding);
        var body = encoding.GetPreamble().Concat(encoding.GetBytes(text.ToString())).ToArray();
        return new AccountingExportFile($"{batch.BatchNumber}-sage-pastel.csv", $"text/csv; charset={encoding.WebName}", body);
    }

    private static string Value(AccountingFileColumn column, AccountingBatchLine line, AccountingAccountMapping? mapping, AccountingFileLayout layout)
    {
        string Number(decimal value, string format = "0.00") => value.ToString(format, CultureInfo.InvariantCulture)
            .Replace(".", layout.DecimalSeparator, StringComparison.Ordinal);
        return column.Field.ToUpperInvariant() switch
        {
            "ENTRY_TYPE" => mapping?.EntryTypeCode ?? string.Empty,
            "CASH_BOOK" => mapping?.CashBookCode ?? string.Empty,
            "DATE" => line.TransactionDate.ToString(layout.DateFormat, CultureInfo.InvariantCulture),
            "REFERENCE" => line.Reference,
            "ACCOUNT" => line.AccountCode,
            "DESCRIPTION" => line.Description,
            "DEBIT" => Number(line.Debit),
            "CREDIT" => Number(line.Credit),
            "AMOUNT" => Number(line.Debit > 0 ? line.Debit : -line.Credit),
            "DC_FLAG" => line.Debit > 0 ? "D" : "C",
            "PROJECT" => line.ProjectCode ?? mapping?.ProjectCode ?? string.Empty,
            "TAX_TYPE" => line.TaxTypeCode ?? mapping?.TaxTypeCode ?? string.Empty,
            "CURRENCY" => line.CurrencyCode,
            "ORIGINAL_AMOUNT" => Number(line.OriginalCurrencyAmount),
            "EXCHANGE_RATE" => Number(line.ExchangeRate, "0.############"),
            "SOURCE_TYPE" => line.SourceType,
            "SOURCE_ID" => line.SourceId.ToString("D"),
            "CONSTANT" => column.ConstantValue ?? string.Empty,
            _ => string.Empty
        };
    }

    private static Encoding ExportEncoding(string name)
    {
        if (name.Equals("WINDOWS_1252", StringComparison.OrdinalIgnoreCase))
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }
        return name.Equals("UTF8_NO_BOM", StringComparison.OrdinalIgnoreCase)
            ? new UTF8Encoding(false, true) : new UTF8Encoding(true, true);
    }

    private static string Csv(string? value, char delimiter)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && "=+-@".Contains(safe[0])) safe = "'" + safe;
        return safe.IndexOfAny(new[] { delimiter, '"', '\r', '\n' }) >= 0
            ? $"\"{safe.Replace("\"", "\"\"")}\"" : safe;
    }
}

public sealed class AccountingIntegrationService(
    AppDbContext db,
    IEnumerable<IAccountingExportAdapter> adapters,
    IDataProtectionProvider dataProtection) : IAccountingIntegrationService
{
    private readonly IDataProtector locationProtector = dataProtection.CreateProtector("eMilimo.AccountingIntegration.FileLocations.v1");
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "DRAFT", "APPROVED", "EXPORTED", "RECONCILED", "PARTIALLY_RECONCILED", "IMPORT_REJECTED", "CANCELLED" };

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
        await ValidateExternalMappingCodesAsync(input, cancellationToken);
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

    public async Task<IReadOnlyList<AccountingExternalCode>> GetExternalCodesAsync(Guid profileId, string? codeType, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.AccountingExternalCodes.AsNoTracking().Where(x => x.AccountingIntegrationProfileId == profileId);
        if (!string.IsNullOrWhiteSpace(codeType))
        {
            var normalized = NormalizeCodeType(codeType);
            query = query.Where(x => x.CodeType == normalized);
        }
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.CodeType).ThenBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<AccountingExternalCode> SaveExternalCodeAsync(Guid? id, AccountingExternalCodeInput input, Guid actorId, CancellationToken cancellationToken)
    {
        _ = await db.AccountingIntegrationProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.AccountingIntegrationProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        var type = NormalizeCodeType(input.CodeType);
        var code = RequireCode(input.Code);
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ArgumentException("Pastel code name is required.");
        AccountingExternalCode entity;
        if (id.HasValue)
        {
            entity = await db.AccountingExternalCodes.FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Pastel code was not found.");
            EnsureVersion(entity.Version, input.Version);
            if (entity.AccountingIntegrationProfileId != input.AccountingIntegrationProfileId)
                throw new ArgumentException("A Pastel code cannot be moved between integration profiles.");
            entity.UpdatedById = actorId; entity.UpdatedDate = DateTime.UtcNow; entity.Version++;
        }
        else
        {
            entity = new AccountingExternalCode
            {
                Id = Guid.NewGuid(), AccountingIntegrationProfileId = input.AccountingIntegrationProfileId,
                CreatedById = actorId, UpdatedById = actorId
            };
            db.AccountingExternalCodes.Add(entity);
        }
        entity.CodeType = type; entity.Code = code; entity.Name = input.Name.Trim();
        entity.Description = Clean(input.Description); entity.Category = Clean(input.Category)?.ToUpperInvariant();
        entity.AllowsDebit = input.AllowsDebit; entity.AllowsCredit = input.AllowsCredit; entity.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<AccountingCodeImportResult> ImportExternalCodesAsync(Guid profileId, Stream source, string fileName, Guid actorId, CancellationToken cancellationToken)
    {
        _ = await db.AccountingIntegrationProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Upload a CSV or TXT Pastel code catalogue.");
        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > 2 * 1024 * 1024) throw new ArgumentException("The code catalogue must be between 1 byte and 2 MB.");
        var bytes = buffer.ToArray();
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            text = Encoding.GetEncoding(1252).GetString(bytes);
        }
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (lines.Count < 2) throw new ArgumentException("The catalogue requires a header and at least one data row.");
        var delimiter = DetectDelimiter(lines.Take(5));
        var header = ParseDelimitedRow(lines[0], delimiter).Select(x => x.Trim().ToUpperInvariant().Replace(" ", "_")).ToList();
        var required = new[] { "CODE_TYPE", "CODE", "NAME" };
        if (required.Any(x => !header.Contains(x))) throw new ArgumentException("Catalogue headers must include CodeType, Code and Name.");
        int Index(params string[] names) => names.Select(name => header.IndexOf(name)).Where(x => x >= 0).DefaultIfEmpty(-1).First();
        string Cell(IReadOnlyList<string> row, int index) => index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        bool Flag(string value, bool fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value == "1";
        var typeIndex = Index("CODE_TYPE", "TYPE"); var codeIndex = Index("CODE"); var nameIndex = Index("NAME");
        var descriptionIndex = Index("DESCRIPTION"); var categoryIndex = Index("CATEGORY");
        var debitIndex = Index("ALLOWS_DEBIT"); var creditIndex = Index("ALLOWS_CREDIT"); var activeIndex = Index("IS_ACTIVE", "ACTIVE");
        var added = 0; var updated = 0; var skipped = 0; var warnings = new List<string>();
        for (var lineNumber = 2; lineNumber <= lines.Count; lineNumber++)
        {
            try
            {
                var row = ParseDelimitedRow(lines[lineNumber - 1], delimiter);
                if (row.Count != header.Count) throw new ArgumentException("Column count does not match the header.");
                var type = NormalizeCodeType(Cell(row, typeIndex)); var code = RequireCode(Cell(row, codeIndex));
                var name = Cell(row, nameIndex); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
                var entity = await db.AccountingExternalCodes.FirstOrDefaultAsync(x => x.AccountingIntegrationProfileId == profileId && x.CodeType == type && x.Code == code, cancellationToken);
                if (entity is null)
                {
                    entity = new AccountingExternalCode { Id = Guid.NewGuid(), AccountingIntegrationProfileId = profileId, CodeType = type, Code = code, CreatedById = actorId };
                    db.AccountingExternalCodes.Add(entity); added++;
                }
                else updated++;
                entity.Name = name; entity.Description = Clean(Cell(row, descriptionIndex)); entity.Category = Clean(Cell(row, categoryIndex))?.ToUpperInvariant();
                entity.AllowsDebit = Flag(Cell(row, debitIndex), true); entity.AllowsCredit = Flag(Cell(row, creditIndex), true);
                entity.IsActive = Flag(Cell(row, activeIndex), true); entity.UpdatedById = actorId; entity.UpdatedDate = DateTime.UtcNow; entity.Version++;
            }
            catch (Exception ex) when (ex is ArgumentException)
            {
                skipped++; warnings.Add($"Line {lineNumber}: {ex.Message}");
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return new AccountingCodeImportResult(added, updated, skipped, warnings.Take(100).ToList());
    }

    public async Task<AccountingExportFile> ExportExternalCodesAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var codes = await GetExternalCodesAsync(profileId, null, true, cancellationToken);
        var text = new StringBuilder("CodeType,Code,Name,Description,Category,AllowsDebit,AllowsCredit,IsActive\r\n");
        foreach (var code in codes)
            text.AppendLine(string.Join(',', CsvValue(code.CodeType), CsvValue(code.Code), CsvValue(code.Name), CsvValue(code.Description), CsvValue(code.Category),
                code.AllowsDebit ? "true" : "false", code.AllowsCredit ? "true" : "false", code.IsActive ? "true" : "false"));
        var encoding = new UTF8Encoding(true);
        return new AccountingExportFile($"pastel-codes-{profileId:N}.csv", "text/csv; charset=utf-8", encoding.GetPreamble().Concat(encoding.GetBytes(text.ToString())).ToArray());
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

    public async Task<AccountingFileLocations> GetFileLocationsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await db.AccountingIntegrationProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        if (!profile.IntegrationSettingId.HasValue) return EmptyLocations();
        var protectedValue = await db.IntegrationSettings.AsNoTracking()
            .Where(x => x.Id == profile.IntegrationSettingId.Value).Select(x => x.ProtectedSettings).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(protectedValue)) return EmptyLocations();
        try
        {
            return JsonSerializer.Deserialize<AccountingFileLocations>(locationProtector.Unprotect(protectedValue)) ?? EmptyLocations();
        }
        catch (Exception)
        {
            throw new InvalidOperationException("The saved accounting file-location configuration cannot be read. Re-save the locations before exporting.");
        }
    }

    public async Task<AccountingFileLocations> SaveFileLocationsAsync(Guid profileId, AccountingFileLocations locations, Guid actorId, CancellationToken cancellationToken)
    {
        var profile = await db.AccountingIntegrationProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        var existing = await GetFileLocationsAsync(profileId, cancellationToken);
        var normalized = new AccountingFileLocations(
            ValidateLocation(locations.ExportPath, "Export"),
            ValidateLocation(locations.ImportPath, "Import-response"),
            ValidateLocation(locations.ArchivePath, "Archive"),
            locations.CreateMissingDirectories,
            locations.ExportLayout ?? existing.ExportLayout);
        var setting = profile.IntegrationSettingId.HasValue
            ? await db.IntegrationSettings.FirstOrDefaultAsync(x => x.Id == profile.IntegrationSettingId.Value, cancellationToken)
            : null;
        if (setting is null)
        {
            setting = new IntegrationSetting
            {
                Id = Guid.NewGuid(), IntegrationKey = $"accounting-file-locations:{profile.Id:N}",
                CreatedById = actorId, UpdatedById = actorId, CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow
            };
            db.IntegrationSettings.Add(setting);
            profile.IntegrationSettingId = setting.Id;
        }
        setting.ProtectedSettings = locationProtector.Protect(JsonSerializer.Serialize(normalized));
        setting.UpdatedById = actorId;
        setting.UpdatedDate = DateTime.UtcNow;
        setting.Version++;
        profile.UpdatedById = actorId;
        profile.UpdatedDate = DateTime.UtcNow;
        profile.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<AccountingFileLayout> GetFileLayoutAsync(Guid profileId, CancellationToken cancellationToken) =>
        (await GetFileLocationsAsync(profileId, cancellationToken)).ExportLayout ?? DefaultFileLayout();

    public async Task<AccountingFileLayout> SaveFileLayoutAsync(Guid profileId, AccountingFileLayout layout, Guid actorId, CancellationToken cancellationToken)
    {
        ValidateFileLayout(layout);
        var locations = await GetFileLocationsAsync(profileId, cancellationToken);
        var normalized = layout with
        {
            Delimiter = layout.Delimiter.Trim().ToUpperInvariant(),
            Encoding = layout.Encoding.Trim().ToUpperInvariant(),
            DebitCreditMode = layout.DebitCreditMode.Trim().ToUpperInvariant(),
            Columns = layout.Columns.OrderBy(x => x.Position)
                .Select((x, i) => x with { Position = i + 1, Field = x.Field.Trim().ToUpperInvariant(), Header = x.Header.Trim() }).ToList()
        };
        var updated = locations with { ExportLayout = normalized };
        await SaveFileLocationsAsync(profileId, updated, actorId, cancellationToken);
        db.AccountingIntegrationProfileAudits.Add(new AccountingIntegrationProfileAud
        {
            Id = Guid.NewGuid(), EntityId = profileId, Action = "FILE_LAYOUT_CONFIGURED",
            ChangesJson = JsonSerializer.Serialize(new
            {
                normalized.Delimiter, normalized.IncludeHeader, normalized.DateFormat, normalized.DecimalSeparator,
                normalized.Encoding, normalized.DebitCreditMode, Columns = normalized.Columns.Select(x => new { x.Position, x.Field, x.Header }),
                normalized.SampleFileName, normalized.SampleSha256
            }),
            PerformedById = actorId, PerformedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    public async Task<AccountingLayoutAnalysis> AnalyzeLayoutSampleAsync(Guid profileId, Stream sample, string fileName, Guid actorId, CancellationToken cancellationToken)
    {
        _ = await db.AccountingIntegrationProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Upload a CSV or TXT sample exported from Sage Pastel Partner.");
        await using var buffer = new MemoryStream();
        await sample.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > 1024 * 1024) throw new ArgumentException("The sample must be between 1 byte and 1 MB.");
        var bytes = buffer.ToArray();
        var encodingName = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? "UTF8_BOM" : "UTF8_NO_BOM";
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            text = Encoding.GetEncoding(1252).GetString(bytes);
            encodingName = "WINDOWS_1252";
        }
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Take(12).ToList();
        if (lines.Count == 0) throw new ArgumentException("The sample contains no readable rows.");
        var delimiter = DetectDelimiter(lines);
        var rows = lines.Select(x => ParseDelimitedRow(x, delimiter)).ToList();
        var width = rows.Max(x => x.Count);
        if (width is < 2 or > 30) throw new ArgumentException("The sample must contain between 2 and 30 columns.");
        if (rows.Any(x => x.Count != width)) throw new ArgumentException("The sample has inconsistent column counts and cannot be inferred safely.");
        var includeHeader = LooksLikeHeader(rows[0]);
        var headers = includeHeader ? rows[0] : Enumerable.Range(1, width).Select(x => $"Column {x}").ToList();
        var columns = headers.Select((header, index) => new AccountingFileColumn(index + 1, SuggestedField(header), header)).ToList();
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var layout = new AccountingFileLayout(DelimiterName(delimiter), includeHeader, "yyyy-MM-dd", ".", encodingName,
            columns.Any(x => x.Field == "AMOUNT") ? "SIGNED_AMOUNT" : "SEPARATE_DEBIT_CREDIT", columns,
            Path.GetFileName(fileName), hash, DateTime.UtcNow);
        var warnings = new List<string>();
        if (columns.Any(x => x.Field == "IGNORE")) warnings.Add("Some columns could not be identified. Map them explicitly or leave them ignored.");
        if (!includeHeader) warnings.Add("No header row was detected. Confirm every positional column before saving.");
        db.AccountingIntegrationProfileAudits.Add(new AccountingIntegrationProfileAud
        {
            Id = Guid.NewGuid(), EntityId = profileId, Action = "FILE_LAYOUT_SAMPLE_ANALYSED",
            ChangesJson = JsonSerializer.Serialize(new { FileName = Path.GetFileName(fileName), Sha256 = hash, Delimiter = DelimiterName(delimiter), ColumnCount = width }),
            PerformedById = actorId, PerformedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return new AccountingLayoutAnalysis(layout, rows.Take(6).Select(x => (IReadOnlyList<string>)x).ToList(), warnings);
    }

    public async Task<AccountingLocationTestResult> TestFileLocationAsync(Guid profileId, string locationType, string? candidatePath, CancellationToken cancellationToken)
    {
        var locations = await GetFileLocationsAsync(profileId, cancellationToken);
        var type = locationType.Trim().ToUpperInvariant();
        var configuredPath = type switch
        {
            "EXPORT" => locations.ExportPath,
            "IMPORT" or "IMPORT_RESPONSE" => locations.ImportPath,
            "ARCHIVE" => locations.ArchivePath,
            _ => throw new ArgumentException("Location type must be EXPORT, IMPORT or ARCHIVE.")
        };
        var path = string.IsNullOrWhiteSpace(candidatePath) ? configuredPath : ValidateLocation(candidatePath, type);
        if (string.IsNullOrWhiteSpace(path)) return new(type, path, false, false, "No path has been configured.");
        try
        {
            if (!Directory.Exists(path) && locations.CreateMissingDirectories) Directory.CreateDirectory(path);
            if (!Directory.Exists(path)) return new(type, path, false, false, "The directory does not exist or is unavailable to the eMilimo service account.");
            // Test an actual write because share visibility alone does not prove
            // the service account can deliver or archive interchange files.
            var probe = Path.Combine(path, $".emilimo-write-test-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probe, "eMilimo accounting location test", cancellationToken);
            File.Delete(probe);
            return new(type, path, true, true, IsUnc(path) ? "Network location is available and writable." : "Local location is available and writable.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return new(type, path, Directory.Exists(path), false, "The location is not writable by the eMilimo service account.");
        }
    }

    public async Task<AccountingImportScanResult> ScanImportResponsesAsync(Guid profileId, Guid? actorId, CancellationToken cancellationToken)
    {
        _ = await db.AccountingIntegrationProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken)
            ?? throw new KeyNotFoundException("Accounting integration profile was not found.");
        var locations = await GetFileLocationsAsync(profileId, cancellationToken);
        if (string.IsNullOrWhiteSpace(locations.ImportPath) || string.IsNullOrWhiteSpace(locations.ArchivePath))
            throw new InvalidOperationException("Configure both import-response and archive locations before scanning responses.");
        EnsureDirectory(locations.ImportPath, locations.CreateMissingDirectories, "import-response");
        EnsureDirectory(locations.ArchivePath, locations.CreateMissingDirectories, "archive");

        var files = Directory.GetFiles(locations.ImportPath, "*.emilimo-result.json", SearchOption.TopDirectoryOnly);
        var processed = 0; var skipped = 0; var failed = 0; var messages = new List<string>();
        foreach (var source in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(source);
            try
            {
                var json = await File.ReadAllTextAsync(source, cancellationToken);
                var result = JsonSerializer.Deserialize<AccountingImportResultManifest>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("The result manifest is empty or invalid.");
                var status = result.Status?.Trim().ToUpperInvariant();
                if (status is not ("ACCEPTED" or "PARTIAL" or "REJECTED"))
                    throw new InvalidOperationException("Status must be ACCEPTED, PARTIAL or REJECTED.");
                if (string.IsNullOrWhiteSpace(result.BatchNumber) || result.AcceptedLines < 0 || result.RejectedLines < 0)
                    throw new InvalidOperationException("Batch number and non-negative line counts are required.");

                var batch = await db.AccountingBatches.Include(x => x.Lines)
                    .FirstOrDefaultAsync(x => x.AccountingIntegrationProfileId == profileId && x.BatchNumber == result.BatchNumber.Trim(), cancellationToken)
                    ?? throw new KeyNotFoundException($"Batch {result.BatchNumber} was not found for this profile.");
                if (string.Equals(batch.ImportFileName, fileName, StringComparison.OrdinalIgnoreCase))
                { skipped++; messages.Add($"{fileName}: already processed."); continue; }
                if (batch.Status == "RECONCILED")
                { skipped++; messages.Add($"{fileName}: {batch.BatchNumber} is already fully reconciled."); continue; }
                if (batch.Status is not ("EXPORTED" or "RECONCILED" or "PARTIALLY_RECONCILED" or "IMPORT_REJECTED"))
                    throw new InvalidOperationException($"Batch {batch.BatchNumber} has not been exported.");
                if (result.AcceptedLines + result.RejectedLines != batch.Lines.Count)
                    throw new InvalidOperationException("Accepted and rejected counts must equal the exported line count.");
                if (status == "ACCEPTED" && (result.RejectedLines != 0 || result.AcceptedLines != batch.Lines.Count) ||
                    status == "REJECTED" && (result.AcceptedLines != 0 || result.RejectedLines != batch.Lines.Count) ||
                    status == "PARTIAL" && (result.AcceptedLines == 0 || result.RejectedLines == 0))
                    throw new InvalidOperationException("The line counts do not agree with the result status.");

                var destination = UniqueArchivePath(locations.ArchivePath, fileName);
                File.Copy(source, destination, overwrite: false);
                try
                {
                    batch.ImportStatus = status;
                    batch.Status = status == "ACCEPTED" ? "RECONCILED" : status == "PARTIAL" ? "PARTIALLY_RECONCILED" : "IMPORT_REJECTED";
                    batch.ImportedAtUtc = DateTime.UtcNow; batch.ImportedById = actorId; batch.ImportFileName = fileName;
                    batch.ImportMessage = Clean(result.Message); batch.AcceptedLineCount = result.AcceptedLines; batch.RejectedLineCount = result.RejectedLines;
                    if (!string.IsNullOrWhiteSpace(result.ExternalBatchReference)) batch.ExternalBatchReference = result.ExternalBatchReference.Trim();
                    batch.LastError = status == "ACCEPTED" ? null : batch.ImportMessage ?? $"Connector reported {status}.";
                    batch.UpdatedById = actorId; batch.UpdatedDate = DateTime.UtcNow; batch.Version++;
                    db.AccountingBatchAttachments.Add(new AccountingBatchAtm
                    {
                        Id = Guid.NewGuid(), EntityId = batch.Id, FileName = fileName, FilePath = destination,
                        MimeType = "application/json", Size = new FileInfo(destination).Length, UploadedById = actorId,
                        UploadedAt = DateTime.UtcNow, Description = "Accounting connector import-result evidence",
                        CreatedById = actorId, UpdatedById = actorId
                    });
                    await db.SaveChangesAsync(cancellationToken);
                    File.Delete(source);
                    processed++; messages.Add($"{fileName}: {status.ToLowerInvariant()} for {batch.BatchNumber}.");
                }
                catch
                {
                    if (File.Exists(destination)) File.Delete(destination);
                    throw;
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                failed++; messages.Add($"{fileName}: {ex.Message}");
                db.ChangeTracker.Clear();
            }
        }
        return new(files.Length, processed, skipped, failed, messages);
    }

    public async Task<AccountingOperationsSummary> GetOperationsSummaryAsync(Guid? profileId, CancellationToken cancellationToken)
    {
        var query = db.AccountingBatches.AsNoTracking();
        if (profileId.HasValue) query = query.Where(x => x.AccountingIntegrationProfileId == profileId.Value);
        return new AccountingOperationsSummary(
            await query.CountAsync(x => x.Status == "DRAFT", cancellationToken),
            await query.CountAsync(x => x.Status == "APPROVED", cancellationToken),
            await query.CountAsync(x => x.Status == "EXPORTED" && x.ImportStatus == "NOT_RECEIVED", cancellationToken),
            await query.CountAsync(x => x.Status == "RECONCILED", cancellationToken),
            await query.CountAsync(x => x.Status == "PARTIALLY_RECONCILED", cancellationToken),
            await query.CountAsync(x => x.Status == "IMPORT_REJECTED", cancellationToken),
            await query.MaxAsync(x => x.ExportedAtUtc, cancellationToken),
            await query.MaxAsync(x => x.ImportedAtUtc, cancellationToken));
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
        issues.AddRange(await MappingCodeIssuesAsync(profile.Id, mappings, cancellationToken));
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
        var errors = await MappingCodeIssuesAsync(profile.Id, mappings, cancellationToken);
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
        var locations = await GetFileLocationsAsync(profile.Id, cancellationToken);
        var file = adapter.Export(profile, batch, locations.ExportLayout ?? DefaultFileLayout());
        if (!string.IsNullOrWhiteSpace(locations.ExportPath))
            await WriteExportFileAsync(locations, file, cancellationToken);
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

    private async Task ValidateExternalMappingCodesAsync(AccountingMappingInput input, CancellationToken cancellationToken)
    {
        async Task RequireMaintained(string? value, string type, string label, bool? requireDebit = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var code = value.Trim().ToUpperInvariant();
            var exists = await db.AccountingExternalCodes.AsNoTracking().AnyAsync(x =>
                x.AccountingIntegrationProfileId == input.AccountingIntegrationProfileId && x.CodeType == type && x.Code == code && x.IsActive &&
                (requireDebit == null || requireDebit == true && x.AllowsDebit || requireDebit == false && x.AllowsCredit), cancellationToken);
            if (!exists) throw new ArgumentException($"{label} '{value}' is not active in the selected profile's Pastel code catalogue.");
        }
        await RequireMaintained(input.DebitAccountCode, "GL_ACCOUNT", "Debit account", true);
        await RequireMaintained(input.CreditAccountCode, "GL_ACCOUNT", "Credit account", false);
        await RequireMaintained(input.CashBookCode, "CASH_BOOK", "Cash book");
        await RequireMaintained(input.EntryTypeCode, "ENTRY_TYPE", "Entry type");
        await RequireMaintained(input.ProjectCode, "PROJECT", "Project");
        await RequireMaintained(input.TaxTypeCode, "TAX_TYPE", "Tax type");
    }

    private async Task<List<string>> MappingCodeIssuesAsync(Guid profileId, IEnumerable<AccountingAccountMapping> mappings, CancellationToken cancellationToken)
    {
        var maintained = await db.AccountingExternalCodes.AsNoTracking()
            .Where(x => x.AccountingIntegrationProfileId == profileId && x.IsActive)
            .Select(x => new { x.CodeType, x.Code, x.AllowsDebit, x.AllowsCredit }).ToListAsync(cancellationToken);
        var active = maintained.Select(x => $"{x.CodeType}:{x.Code}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var debitAllowed = maintained.Where(x => x.AllowsDebit).Select(x => $"{x.CodeType}:{x.Code}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var creditAllowed = maintained.Where(x => x.AllowsCredit).Select(x => $"{x.CodeType}:{x.Code}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var issues = new List<string>();
        foreach (var mapping in mappings)
        {
            void Check(string? code, string type, string label)
            {
                if (!string.IsNullOrWhiteSpace(code) && !active.Contains($"{type}:{code.Trim()}"))
                    issues.Add($"{mapping.SourceLabel}: {label} '{code}' is missing or inactive in the Pastel code catalogue.");
            }
            Check(mapping.DebitAccountCode, "GL_ACCOUNT", "debit account");
            Check(mapping.CreditAccountCode, "GL_ACCOUNT", "credit account");
            if (!string.IsNullOrWhiteSpace(mapping.DebitAccountCode) && !debitAllowed.Contains($"GL_ACCOUNT:{mapping.DebitAccountCode.Trim()}"))
                issues.Add($"{mapping.SourceLabel}: account '{mapping.DebitAccountCode}' is not enabled for debit postings.");
            if (!string.IsNullOrWhiteSpace(mapping.CreditAccountCode) && !creditAllowed.Contains($"GL_ACCOUNT:{mapping.CreditAccountCode.Trim()}"))
                issues.Add($"{mapping.SourceLabel}: account '{mapping.CreditAccountCode}' is not enabled for credit postings.");
            Check(mapping.CashBookCode, "CASH_BOOK", "cash book");
            Check(mapping.EntryTypeCode, "ENTRY_TYPE", "entry type");
            Check(mapping.ProjectCode, "PROJECT", "project");
            Check(mapping.TaxTypeCode, "TAX_TYPE", "tax type");
        }
        return issues;
    }

    private static readonly HashSet<string> ExternalCodeTypes = new(StringComparer.OrdinalIgnoreCase)
        { "GL_ACCOUNT", "CASH_BOOK", "ENTRY_TYPE", "TAX_TYPE", "PROJECT", "CURRENCY" };

    private static string NormalizeCodeType(string value)
    {
        var type = value.Trim().ToUpperInvariant().Replace(' ', '_');
        if (!ExternalCodeTypes.Contains(type))
            throw new ArgumentException("Code type must be GL_ACCOUNT, CASH_BOOK, ENTRY_TYPE, TAX_TYPE, PROJECT or CURRENCY.");
        return type;
    }

    private static string RequireCode(string value)
    {
        var code = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 40) throw new ArgumentException("Pastel code is required and cannot exceed 40 characters.");
        if (code.Any(char.IsControl)) throw new ArgumentException("Pastel code contains invalid control characters.");
        return code;
    }

    private static string CsvValue(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && "=+-@".Contains(safe[0])) safe = "'" + safe;
        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static void EnsureVersion(int current, int? supplied)
    {
        if (!supplied.HasValue || supplied.Value != current)
            throw new DbUpdateConcurrencyException("This record changed while you were editing it. Reload and try again.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AccountingFileLocations EmptyLocations() => new(string.Empty, string.Empty, string.Empty, false);

    public static AccountingFileLayout DefaultFileLayout() => new(
        "COMMA", true, "yyyy-MM-dd", ".", "UTF8_BOM", "SEPARATE_DEBIT_CREDIT",
        new List<AccountingFileColumn>
        {
            new(1,"ENTRY_TYPE","EntryType"), new(2,"CASH_BOOK","CashBook"), new(3,"DATE","Date"),
            new(4,"REFERENCE","Reference"), new(5,"ACCOUNT","Account"), new(6,"DESCRIPTION","Description"),
            new(7,"DEBIT","Debit"), new(8,"CREDIT","Credit"), new(9,"PROJECT","Project"),
            new(10,"TAX_TYPE","TaxType"), new(11,"CURRENCY","Currency"), new(12,"ORIGINAL_AMOUNT","OriginalAmount"),
            new(13,"EXCHANGE_RATE","ExchangeRate"), new(14,"SOURCE_TYPE","SourceType"), new(15,"SOURCE_ID","SourceId")
        });

    private static readonly HashSet<string> FileFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "ENTRY_TYPE", "CASH_BOOK", "DATE", "REFERENCE", "ACCOUNT", "DESCRIPTION", "DEBIT", "CREDIT",
        "AMOUNT", "DC_FLAG", "PROJECT", "TAX_TYPE", "CURRENCY", "ORIGINAL_AMOUNT", "EXCHANGE_RATE",
        "SOURCE_TYPE", "SOURCE_ID", "CONSTANT", "IGNORE"
    };

    public static void ValidateFileLayout(AccountingFileLayout layout)
    {
        if (layout.Columns is null || layout.Columns.Count is < 2 or > 30) throw new ArgumentException("The file layout must contain between 2 and 30 columns.");
        _ = DelimiterCharacter(layout.Delimiter);
        if (layout.Encoding.ToUpperInvariant() is not ("UTF8_BOM" or "UTF8_NO_BOM" or "WINDOWS_1252"))
            throw new ArgumentException("Encoding must be UTF8_BOM, UTF8_NO_BOM or WINDOWS_1252.");
        if (layout.DecimalSeparator is not ("." or ",")) throw new ArgumentException("Decimal separator must be a period or comma.");
        if (layout.DateFormat is not ("yyyy-MM-dd" or "dd/MM/yyyy" or "MM/dd/yyyy" or "yyyyMMdd" or "ddMMyyyy"))
            throw new ArgumentException("Select one of the supported date formats.");
        if (layout.DebitCreditMode.ToUpperInvariant() is not ("SEPARATE_DEBIT_CREDIT" or "SIGNED_AMOUNT"))
            throw new ArgumentException("Debit/credit mode must use separate columns or a signed amount.");
        if (layout.Columns.Select(x => x.Position).Distinct().Count() != layout.Columns.Count || layout.Columns.Any(x => x.Position <= 0))
            throw new ArgumentException("File-layout column positions must be unique positive numbers.");
        if (layout.Columns.Any(x => !FileFields.Contains(x.Field))) throw new ArgumentException("The file layout contains an unsupported field.");
        var fields = layout.Columns.Select(x => x.Field.ToUpperInvariant()).ToHashSet();
        foreach (var required in new[] { "DATE", "REFERENCE", "ACCOUNT", "DESCRIPTION" })
            if (!fields.Contains(required)) throw new ArgumentException($"The file layout requires a {required} column.");
        if (layout.DebitCreditMode.Equals("SEPARATE_DEBIT_CREDIT", StringComparison.OrdinalIgnoreCase) && (!fields.Contains("DEBIT") || !fields.Contains("CREDIT")))
            throw new ArgumentException("Separate debit/credit layouts require both DEBIT and CREDIT columns.");
        if (layout.DebitCreditMode.Equals("SIGNED_AMOUNT", StringComparison.OrdinalIgnoreCase) && !fields.Contains("AMOUNT"))
            throw new ArgumentException("Signed-amount layouts require an AMOUNT column.");
    }

    public static char DelimiterCharacter(string name) => name.Trim().ToUpperInvariant() switch
    {
        "COMMA" => ',', "SEMICOLON" => ';', "TAB" => '\t', "PIPE" => '|',
        _ => throw new ArgumentException("Delimiter must be COMMA, SEMICOLON, TAB or PIPE.")
    };

    private static string DelimiterName(char delimiter) => delimiter switch { ';' => "SEMICOLON", '\t' => "TAB", '|' => "PIPE", _ => "COMMA" };

    private static char DetectDelimiter(IEnumerable<string> lines)
    {
        var candidates = new[] { ',', ';', '\t', '|' };
        return candidates.Select(candidate => new
        {
            Character = candidate,
            Score = lines.Take(5).Select(x => ParseDelimitedRow(x, candidate).Count).GroupBy(x => x)
                .OrderByDescending(x => x.Count()).ThenByDescending(x => x.Key).FirstOrDefault()?.Key ?? 1
        }).OrderByDescending(x => x.Score).First().Character;
    }

    private static List<string> ParseDelimitedRow(string line, char delimiter)
    {
        var result = new List<string>(); var value = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { value.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == delimiter && !quoted) { result.Add(value.ToString().Trim()); value.Clear(); }
            else value.Append(c);
        }
        if (quoted) throw new ArgumentException("The sample contains an unterminated quoted value.");
        result.Add(value.ToString().Trim()); return result;
    }

    private static bool LooksLikeHeader(IEnumerable<string> row) => row.Count(x => SuggestedField(x) != "IGNORE") >= 2;

    private static string SuggestedField(string header)
    {
        var key = new string(header.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return key switch
        {
            "ENTRYTYPE" or "TYPE" => "ENTRY_TYPE", "CASHBOOK" or "CASHBOOKCODE" => "CASH_BOOK",
            "DATE" or "TRANSACTIONDATE" => "DATE", "REFERENCE" or "REF" => "REFERENCE",
            "ACCOUNT" or "ACCOUNTCODE" or "GLACCOUNT" => "ACCOUNT", "DESCRIPTION" or "DETAILS" => "DESCRIPTION",
            "DEBIT" or "DEBITAMOUNT" => "DEBIT", "CREDIT" or "CREDITAMOUNT" => "CREDIT",
            "AMOUNT" or "VALUE" => "AMOUNT", "DC" or "DRCR" or "DEBITCREDIT" => "DC_FLAG",
            "PROJECT" or "PROJECTCODE" => "PROJECT", "TAX" or "TAXTYPE" or "TAXCODE" => "TAX_TYPE",
            "CURRENCY" or "CURRENCYCODE" => "CURRENCY", "ORIGINALAMOUNT" => "ORIGINAL_AMOUNT",
            "EXCHANGERATE" or "RATE" => "EXCHANGE_RATE", "SOURCETYPE" => "SOURCE_TYPE", "SOURCEID" => "SOURCE_ID",
            _ => "IGNORE"
        };
    }

    private static string ValidateLocation(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var raw = Environment.ExpandEnvironmentVariables(value.Trim());
        if (raw.StartsWith("\\\\?\\", StringComparison.Ordinal) || raw.StartsWith("\\\\.\\", StringComparison.Ordinal))
            throw new ArgumentException($"{label} location cannot use a Windows device path.");
        if (!Path.IsPathFullyQualified(raw)) throw new ArgumentException($"{label} location must be an absolute local path or UNC network path.");
        var normalized = Path.GetFullPath(raw).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = Path.GetPathRoot(normalized)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!IsUnc(normalized) && string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"{label} location cannot be a local drive root.");
        return normalized;
    }

    private static bool IsUnc(string path) => path.StartsWith("\\\\", StringComparison.Ordinal);

    private static void EnsureDirectory(string path, bool createMissing, string label)
    {
        if (!Directory.Exists(path) && createMissing) Directory.CreateDirectory(path);
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"The configured accounting {label} location is unavailable.");
    }

    private static string UniqueArchivePath(string directory, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var candidate = Path.Combine(directory, safeName);
        if (!File.Exists(candidate)) return candidate;
        return Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(safeName)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(safeName)}");
    }

    private static async Task WriteExportFileAsync(AccountingFileLocations locations, AccountingExportFile file, CancellationToken cancellationToken)
    {
        var directory = locations.ExportPath;
        if (!Directory.Exists(directory))
        {
            if (!locations.CreateMissingDirectories) throw new DirectoryNotFoundException("The configured accounting export location is unavailable.");
            Directory.CreateDirectory(directory);
        }
        var destination = Path.Combine(directory, Path.GetFileName(file.FileName));
        var temporary = destination + $".{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporary, file.Content, cancellationToken);
        File.Move(temporary, destination, overwrite: true);
    }
}
