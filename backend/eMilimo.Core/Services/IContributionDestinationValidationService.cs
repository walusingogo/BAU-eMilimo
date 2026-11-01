using eMilimo.Core.Entities;

namespace eMilimo.Core.Services;

public sealed record DestinationValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string? DestinationType = null,
    Guid? DestinationId = null);

public sealed record EligibleDestinationOption(
    Guid Id,
    string Name,
    string DestinationType);

public interface IContributionDestinationValidationService
{
    /// <summary>
    /// Validates that a contribution type allows allocation to a specific destination.
    /// </summary>
    /// <param name="contributionTypeId">The contribution type being allocated</param>
    /// <param name="destinationType">The destination type: "ORGANIZATION", "SECTION", "GROUP", "COMMITTEE"</param>
    /// <param name="destinationId">The specific destination ID (null for ORGANIZATION)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Validation result with error message if invalid</returns>
    Task<DestinationValidationResult> ValidateDestinationAsync(
        Guid contributionTypeId,
        string destinationType,
        Guid? destinationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets eligible destinations for a contribution type, optionally filtered by member associations.
    /// </summary>
    /// <param name="contributionTypeId">The contribution type</param>
    /// <param name="memberId">Optional: if provided, includes member-associated destinations</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of eligible destinations</returns>
    Task<IReadOnlyList<EligibleDestinationOption>> GetEligibleDestinationsAsync(
        Guid contributionTypeId,
        Guid? memberId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multiple contribution lines in a single payment.
    /// Ensures each line has valid destination and amount.
    /// </summary>
    /// <param name="lines">Payment lines to validate</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of validation errors (empty if all valid)</returns>
    Task<IReadOnlyList<string>> ValidatePaymentLinesAsync(
        IReadOnlyList<PaymentLineToValidate> lines,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentLineToValidate(
    Guid ContributionTypeId,
    decimal Amount,
    string DestinationType,
    Guid? DestinationId);
