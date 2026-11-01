using eMilimo.API.Services;
using eMilimo.Core.Services;
using Xunit;

namespace eMilimo.API.Tests.Services;

public sealed class FinancialRecordBookTests
{
    [Theory]
    [InlineData(1, "CON00120261004")]
    [InlineData(99, "CON09920261004")]
    [InlineData(100, "CON10020261004")]
    [InlineData(999, "CON99920261004")]
    [InlineData(1000, "CON100020261004")]
    public void Reference_format_has_minimum_three_digits_and_no_ceiling(long sequence, string expected)
        => Assert.Equal(expected, ReferenceNumberService.Format("CON", sequence, new DateOnly(2026, 10, 4)));

    [Theory]
    [InlineData("FRB", "FRB00120261004")]
    [InlineData("REQ", "REQ00120261004")]
    [InlineData("AST", "AST00120261004")]
    public void Reference_format_supports_platform_prefixes(string prefix, string expected)
        => Assert.Equal(expected, ReferenceNumberService.Format(prefix, 1, new DateOnly(2026, 10, 4)));

    [Fact]
    public void Percentage_assessment_uses_financial_rounding()
        => Assert.Equal(10.01m, FrbFinancialCalculator.Assessment("PERCENTAGE", 100.05m, 10m, null));

    [Fact]
    public void Fixed_assessment_only_applies_when_contribution_base_exists()
    {
        Assert.Equal(0m, FrbFinancialCalculator.Assessment("FIXED", 0m, null, 250m));
        Assert.Equal(250m, FrbFinancialCalculator.Assessment("FIXED", 1m, null, 250m));
    }

    [Fact]
    public void Currency_rounds_half_away_from_zero()
        => Assert.Equal(10.01m, FrbFinancialCalculator.Money(10.005m));

    [Fact]
    public void Full_distribution_calculates_every_line_against_the_same_base()
    {
        var amounts = FrbFinancialCalculator.Distribution("PERCENTAGE", 20_000m,
            [(60m, null), (10m, null), (20m, null), (10m, null)]);

        Assert.Equal([12_000m, 2_000m, 4_000m, 2_000m], amounts);
        Assert.Equal(20_000m, amounts.Sum());
    }

    [Fact]
    public void Local_allocation_is_retained_while_only_external_allocations_reduce_position()
        => Assert.Equal(12_000m, FrbFinancialCalculator.CongregationalPosition(20_000m, 0m, 0m, 8_000m));

    [Fact]
    public void Active_full_allocation_must_total_exactly_one_hundred_percent()
    {
        var lines = Lines(("Congregation", 60m, true), ("Consistory", 10m, false), ("Presbytery", 20m, false), ("Synod", 9.99m, false));
        Assert.Contains("100%", FrbFinancialCalculator.ValidateScheme("ALLOCATION", "PERCENTAGE", true, lines));
    }

    [Fact]
    public void Inactive_incomplete_allocation_can_be_saved_for_configuration_work()
        => Assert.Null(FrbFinancialCalculator.ValidateScheme("ALLOCATION", "PERCENTAGE", false, Lines(("Congregation", 99.99m, true))));

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Percentage_outside_permitted_range_is_rejected(double rate)
        => Assert.Contains("between 0 and 100", FrbFinancialCalculator.ValidateScheme("DEDUCTION", "PERCENTAGE", true, Lines(("Destination", (decimal)rate, false))));

    [Fact]
    public void Duplicate_beneficiary_is_rejected_case_insensitively()
        => Assert.Contains("cannot appear more than once", FrbFinancialCalculator.ValidateScheme("ALLOCATION", "PERCENTAGE", true,
            Lines(("Congregation", 60m, true), ("congregation", 40m, false))));

    [Fact]
    public void Missing_beneficiary_is_rejected()
        => Assert.Contains("requires a beneficiary", FrbFinancialCalculator.ValidateScheme("DEDUCTION", "PERCENTAGE", true, Lines((" ", 20m, false))));

    [Fact]
    public void Allocation_requires_at_least_one_line()
        => Assert.Contains("at least one", FrbFinancialCalculator.ValidateScheme("ALLOCATION", "PERCENTAGE", true, []));

    [Fact]
    public void Simple_deduction_remains_supported_without_a_one_hundred_percent_total()
        => Assert.Null(FrbFinancialCalculator.ValidateScheme("DEDUCTION", "PERCENTAGE", true, Lines(("Presbytery", 20m, false))));

    [Fact]
    public void Simple_addition_with_fixed_amount_remains_supported()
    {
        var lines = new List<(string Beneficiary, decimal? Rate, decimal? FixedAmount, bool IsLocalRetained)>
            { ("Grant", null, 500m, false) };
        Assert.Null(FrbFinancialCalculator.ValidateScheme("ADDITION", "FIXED", true, lines));
        Assert.Equal(500m, FrbFinancialCalculator.Distribution("FIXED", 1m, [(null, 500m)]).Single());
    }

    [Fact]
    public void Eligible_contribution_query_dates_are_normalized_to_utc_day_boundaries()
    {
        var (start, end) = FrbQueryDateRange.InclusiveDays(new DateTime(2026, 1, 1), new DateTime(2026, 10, 4));
        Assert.Equal(DateTimeKind.Utc, start.Kind);
        Assert.Equal(DateTimeKind.Utc, end.Kind);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), end);
    }

    private static List<(string Beneficiary, decimal? Rate, decimal? FixedAmount, bool IsLocalRetained)> Lines(
        params (string Beneficiary, decimal Rate, bool IsLocalRetained)[] lines) =>
        lines.Select(line => (line.Beneficiary, (decimal?)line.Rate, (decimal?)null, line.IsLocalRetained)).ToList();
}
