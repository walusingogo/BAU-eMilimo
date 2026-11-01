namespace eMilimo.Core.Services;

/// <summary>Single deterministic currency calculation policy used by the FRB workflow.</summary>
public static class FrbFinancialCalculator
{
    public static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal Assessment(string method, decimal assessmentBase, decimal? rate, decimal? fixedAmount) =>
        method.Trim().ToUpperInvariant() switch
        {
            "PERCENTAGE" => Money(assessmentBase * (rate ?? 0m) / 100m),
            "FIXED" => assessmentBase > 0 ? Money(fixedAmount ?? 0m) : 0m,
            _ => throw new InvalidOperationException($"Unsupported FRB calculation method '{method}'.")
        };

    public static IReadOnlyList<decimal> Distribution(
        string method, decimal assessmentBase, IEnumerable<(decimal? Rate, decimal? FixedAmount)> lines) =>
        lines.Select(line => Assessment(method, assessmentBase, line.Rate, line.FixedAmount)).ToList();

    public static decimal CongregationalPosition(decimal gross, decimal additions, decimal deductions, decimal externalAllocations) =>
        Money(gross + additions - deductions - externalAllocations);

    public static string? ValidateScheme(
        string classification, string method, bool isActive,
        IReadOnlyCollection<(string Beneficiary, decimal? Rate, decimal? FixedAmount, bool IsLocalRetained)> lines)
    {
        classification = classification.Trim().ToUpperInvariant(); method = method.Trim().ToUpperInvariant();
        if (method is not ("PERCENTAGE" or "FIXED")) return "Calculation method must be Percentage or Fixed.";
        if (classification is not ("DEDUCTION" or "ADDITION" or "ALLOCATION")) return "Classification must be Deduction, Addition, or Allocation.";
        if (lines.Count == 0) return classification == "ALLOCATION" ? "Add at least one allocation line." : "Add one beneficiary / destination line.";
        if (classification != "ALLOCATION" && lines.Count != 1) return "Addition and Deduction schemes must contain exactly one destination line.";
        if (lines.Any(line => string.IsNullOrWhiteSpace(line.Beneficiary))) return "Every allocation line requires a beneficiary / destination.";
        if (lines.GroupBy(line => line.Beneficiary.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1)) return "A beneficiary / destination cannot appear more than once in the same scheme.";
        if (lines.Count(line => line.IsLocalRetained) > 1) return "Only one allocation line can be marked as the local retained share.";
        if (method == "PERCENTAGE" && lines.Any(line => !line.Rate.HasValue || line.Rate < 0 || line.Rate > 100)) return "Every percentage must be between 0 and 100.";
        if (method == "FIXED" && lines.Any(line => !line.FixedAmount.HasValue || line.FixedAmount < 0)) return "Every fixed amount must be zero or greater.";
        if (classification == "ALLOCATION" && method != "PERCENTAGE") return "Full allocation schemes must use the Percentage calculation method.";
        if (classification == "ALLOCATION" && isActive && lines.Sum(line => line.Rate ?? 0m) != 100m) return "An active full allocation scheme must total exactly 100%.";
        return null;
    }
}

/// <summary>Normalizes date-only FRB filters before they are sent to PostgreSQL timestamptz columns.</summary>
public static class FrbQueryDateRange
{
    public static (DateTime StartUtc, DateTime EndExclusiveUtc) InclusiveDays(DateTime from, DateTime to) =>
        (DateTime.SpecifyKind(from.Date, DateTimeKind.Utc), DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc));
}
