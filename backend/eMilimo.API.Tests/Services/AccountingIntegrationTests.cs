using System.Text;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class AccountingIntegrationTests
{
    [Fact]
    public void SageExport_ProducesBalancedTraceableCsvWithUtf8Bom()
    {
        var sourceId = Guid.NewGuid();
        var batch = new AccountingBatch
        {
            BatchNumber = "ACC-TEST-001",
            DebitTotal = 100m,
            CreditTotal = 100m,
            Lines =
            [
                Line(1, sourceId, "1000", 100m, 0m, "Receipt"),
                Line(2, sourceId, "4000", 0m, 100m, "Tithe")
            ]
        };
        var profile = new AccountingIntegrationProfile
        {
            ProviderKey = "SAGE_PASTEL_PARTNER",
            Mappings =
            [
                new AccountingAccountMapping { DebitAccountCode = "1000", EntryTypeCode = "REC", CashBookCode = "CB1", IsActive = true },
                new AccountingAccountMapping { CreditAccountCode = "4000", EntryTypeCode = "GJ", IsActive = true }
            ]
        };

        var file = new SagePastelFileExportAdapter().Export(profile, batch);

        Assert.Equal(Encoding.UTF8.Preamble.ToArray(), file.Content.Take(3).ToArray());
        var csv = Encoding.UTF8.GetString(file.Content);
        Assert.Contains("SourceType,SourceId", csv);
        Assert.Contains(sourceId.ToString("D"), csv);
        Assert.Contains("100.00,0.00", csv);
        Assert.Contains("0.00,100.00", csv);
    }

    [Fact]
    public void SageExport_NeutralizesSpreadsheetFormulaValues()
    {
        var batch = new AccountingBatch
        {
            BatchNumber = "ACC-TEST-002",
            Lines = [Line(1, Guid.NewGuid(), "1000", 1m, 0m, "=HYPERLINK(\"unsafe\")")]
        };
        var file = new SagePastelFileExportAdapter().Export(new AccountingIntegrationProfile(), batch);
        var csv = Encoding.UTF8.GetString(file.Content);
        Assert.Contains("'=HYPERLINK", csv);
    }

    [Fact]
    public void SageExport_UsesConfiguredLegacyPartnerLayout()
    {
        var batch = new AccountingBatch
        {
            BatchNumber = "ACC-TEST-003",
            Lines = [Line(1, Guid.NewGuid(), "1000", 125.50m, 0m, "Receipt")]
        };
        var layout = new AccountingFileLayout(
            "SEMICOLON", false, "dd/MM/yyyy", ",", "WINDOWS_1252", "SIGNED_AMOUNT",
            [new(1, "DATE", "Date"), new(2, "REFERENCE", "Reference"), new(3, "ACCOUNT", "Account"),
             new(4, "DESCRIPTION", "Description"), new(5, "AMOUNT", "Amount"), new(6, "CONSTANT", "Batch", "LWZ")]);

        var file = new SagePastelFileExportAdapter().Export(new AccountingIntegrationProfile(), batch, layout);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var text = Encoding.GetEncoding(1252).GetString(file.Content);

        Assert.DoesNotContain("Date;Reference", text);
        Assert.Contains("01/11/2026;CON-001;1000;Receipt;125,50;LWZ", text);
    }

    [Fact]
    public void FileLayout_RejectsMissingFinancialColumns()
    {
        var layout = new AccountingFileLayout("COMMA", true, "yyyy-MM-dd", ".", "UTF8_BOM", "SEPARATE_DEBIT_CREDIT",
            [new(1, "DATE", "Date"), new(2, "REFERENCE", "Reference"), new(3, "ACCOUNT", "Account"), new(4, "DESCRIPTION", "Description")]);

        var error = Assert.Throws<ArgumentException>(() => AccountingIntegrationService.ValidateFileLayout(layout));
        Assert.Contains("DEBIT", error.Message);
    }

    private static AccountingBatchLine Line(int number, Guid sourceId, string account, decimal debit, decimal credit, string description) => new()
    {
        Id = Guid.NewGuid(), LineNumber = number, SourceType = "CONTRIBUTION", SourceId = sourceId,
        SourceReference = "CON-001", TransactionDate = new DateOnly(2026, 11, 1), AccountCode = account,
        Debit = debit, Credit = credit, Description = description, Reference = "CON-001", CurrencyCode = "ZMW",
        OriginalCurrencyAmount = debit + credit, ExchangeRate = 1m
    };
}
