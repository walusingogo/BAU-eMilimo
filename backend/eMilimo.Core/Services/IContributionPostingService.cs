namespace eMilimo.Core.Services;

public sealed record ContributionPostingRequest(
    Guid ContributionId,
    Guid? ActorUserId,
    DateTime ConfirmedAtUtc,
    string Trigger);

public sealed record ContributionPostingResult(
    Guid ContributionId,
    bool AlreadyPosted,
    int LedgerEntryCount,
    decimal PostedAmount);

/// <summary>
/// Posts a validated contribution into the church fund ledger. The caller owns
/// the database transaction so contribution creation and payment verification
/// can later be committed atomically with the financial posting.
/// </summary>
public interface IContributionPostingService
{
    Task<ContributionPostingResult> PostAsync(
        ContributionPostingRequest request,
        CancellationToken cancellationToken = default);
}
