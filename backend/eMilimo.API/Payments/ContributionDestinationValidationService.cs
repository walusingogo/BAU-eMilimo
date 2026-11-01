using eMilimo.Core.Entities;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class ContributionDestinationValidationService : IContributionDestinationValidationService
{
    private readonly AppDbContext _db;

    public ContributionDestinationValidationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DestinationValidationResult> ValidateDestinationAsync(
        Guid contributionTypeId,
        string destinationType,
        Guid? destinationId,
        CancellationToken cancellationToken = default)
    {
        // Get the contribution type
        var contributionType = await _db.ContributionTypes
            .FirstOrDefaultAsync(x => x.Id == contributionTypeId && x.IsActive, cancellationToken);
        if (contributionType is null)
            return new DestinationValidationResult(false, "Contribution type not found or inactive.");

        // Validate destination type matches allowed allocations
        switch (destinationType.ToUpperInvariant())
        {
            case "ORGANIZATION":
                if (!contributionType.AllowOrganizationWideAllocation)
                    return new DestinationValidationResult(false, $"Contribution type '{contributionType.Name}' does not allow organization-wide allocation.");
                if (destinationId.HasValue && destinationId.Value != Guid.Empty)
                    return new DestinationValidationResult(false, "Organization-wide allocation must not include a section, group, or committee ID.");
                return new DestinationValidationResult(true, null, "ORGANIZATION", null);

            case "SECTION":
                if (!contributionType.AllowSectionAllocation)
                    return new DestinationValidationResult(false, $"Contribution type '{contributionType.Name}' does not allow section allocation.");
                if (!destinationId.HasValue)
                    return new DestinationValidationResult(false, "Section ID is required for section allocation.");
                var section = await _db.Sections
                    .FirstOrDefaultAsync(x => x.Id == destinationId && x.IsActive, cancellationToken);
                if (section is null)
                    return new DestinationValidationResult(false, "Selected section not found or inactive.");
                return new DestinationValidationResult(true, null, "SECTION", destinationId);

            case "GROUP":
                if (!contributionType.AllowGroupAllocation)
                    return new DestinationValidationResult(false, $"Contribution type '{contributionType.Name}' does not allow group allocation.");
                if (!destinationId.HasValue)
                    return new DestinationValidationResult(false, "Group ID is required for group allocation.");
                var group = await _db.Groups
                    .FirstOrDefaultAsync(x => x.Id == destinationId && x.IsActive, cancellationToken);
                if (group is null)
                    return new DestinationValidationResult(false, "Selected group not found or inactive.");
                return new DestinationValidationResult(true, null, "GROUP", destinationId);

            case "COMMITTEE":
                if (!contributionType.AllowCommitteeAllocation)
                    return new DestinationValidationResult(false, $"Contribution type '{contributionType.Name}' does not allow committee allocation.");
                if (!destinationId.HasValue)
                    return new DestinationValidationResult(false, "Committee ID is required for committee allocation.");
                var committee = await _db.Committees
                    .FirstOrDefaultAsync(x => x.Id == destinationId && x.IsActive, cancellationToken);
                if (committee is null)
                    return new DestinationValidationResult(false, "Selected committee not found or inactive.");
                return new DestinationValidationResult(true, null, "COMMITTEE", destinationId);

            default:
                return new DestinationValidationResult(false, $"Unknown destination type: {destinationType}");
        }
    }

    public async Task<IReadOnlyList<EligibleDestinationOption>> GetEligibleDestinationsAsync(
        Guid contributionTypeId,
        Guid? memberId = null,
        CancellationToken cancellationToken = default)
    {
        var contributionType = await _db.ContributionTypes
            .FirstOrDefaultAsync(x => x.Id == contributionTypeId && x.IsActive, cancellationToken);
        if (contributionType is null)
            return Array.Empty<EligibleDestinationOption>();

        var options = new List<EligibleDestinationOption>();

        // Add organization-wide if allowed
        if (contributionType.AllowOrganizationWideAllocation)
        {
            options.Add(new EligibleDestinationOption(Guid.Empty, "Organization-wide", "ORGANIZATION"));
        }

        // Add sections if allowed
        if (contributionType.AllowSectionAllocation)
        {
            var sections = await _db.Sections.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SectionNumber)
                .ToListAsync(cancellationToken);
            options.AddRange(sections.Select(s => new EligibleDestinationOption(s.Id, s.SectionName, "SECTION")));
        }

        // Add groups if allowed
        if (contributionType.AllowGroupAllocation)
        {
            IQueryable<Group> groupsQuery = _db.Groups.AsNoTracking().Where(x => x.IsActive);

            // If member ID provided and policy is MEMBER_ASSOCIATED, filter groups
            if (memberId.HasValue && contributionType.DigitalDestinationEligibilityPolicy == ContributionDestinationEligibilityPolicies.MemberAssociated)
            {
                var member = await _db.Members
                    .FirstOrDefaultAsync(x => x.Id == memberId, cancellationToken);
                if (member is not null)
                {
                    // Get member's group assignments
                    var memberGroupIds = await _db.UserGroupAssignments
                        .Where(x => x.UserId == member.Id)
                        .Select(x => x.GroupId)
                        .ToListAsync(cancellationToken);
                    groupsQuery = groupsQuery.Where(x => memberGroupIds.Contains(x.Id));
                }
            }

            var groups = await groupsQuery.OrderBy(x => x.GroupName).ToListAsync(cancellationToken);
            options.AddRange(groups.Select(g => new EligibleDestinationOption(g.Id, g.GroupName, "GROUP")));
        }

        // Add committees if allowed
        if (contributionType.AllowCommitteeAllocation)
        {
            IQueryable<Committee> committeesQuery = _db.Committees.AsNoTracking().Where(x => x.IsActive);

            // If member ID provided and policy is MEMBER_ASSOCIATED, filter committees
            if (memberId.HasValue && contributionType.DigitalDestinationEligibilityPolicy == ContributionDestinationEligibilityPolicies.MemberAssociated)
            {
                var member = await _db.Members
                    .FirstOrDefaultAsync(x => x.Id == memberId, cancellationToken);
                if (member is not null)
                {
                    // Get member's committee assignments
                    var memberCommitteeIds = await _db.UserCommitteeAssignments
                        .Where(x => x.UserId == member.Id)
                        .Select(x => x.CommitteeId)
                        .ToListAsync(cancellationToken);
                    committeesQuery = committeesQuery.Where(x => memberCommitteeIds.Contains(x.Id));
                }
            }

            var committees = await committeesQuery.OrderBy(x => x.CommitteeName).ToListAsync(cancellationToken);
            options.AddRange(committees.Select(c => new EligibleDestinationOption(c.Id, c.CommitteeName, "COMMITTEE")));
        }

        return options;
    }

    public async Task<IReadOnlyList<string>> ValidatePaymentLinesAsync(
        IReadOnlyList<PaymentLineToValidate> lines,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (!lines.Any())
        {
            errors.Add("Payment must include at least one contribution line.");
            return errors;
        }

        var contributionTypeIds = lines.Select(x => x.ContributionTypeId).Distinct().ToList();
        var contributionTypes = await _db.ContributionTypes
            .Where(x => contributionTypeIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineNum = i + 1;

            if (line.Amount <= 0)
            {
                errors.Add($"Line {lineNum}: Amount must be greater than zero.");
                continue;
            }

            var contribType = contributionTypes.FirstOrDefault(x => x.Id == line.ContributionTypeId);
            if (contribType is null)
            {
                errors.Add($"Line {lineNum}: Contribution type not found or inactive.");
                continue;
            }

            if (contribType.MinimumAmount.HasValue && line.Amount < contribType.MinimumAmount.Value)
            {
                errors.Add($"Line {lineNum}: Amount must be at least {contribType.MinimumAmount:C}.");
            }

            if (contribType.MaximumAmount.HasValue && line.Amount > contribType.MaximumAmount.Value)
            {
                errors.Add($"Line {lineNum}: Amount cannot exceed {contribType.MaximumAmount:C}.");
            }

            var destinationValidation = await ValidateDestinationAsync(
                line.ContributionTypeId,
                line.DestinationType,
                line.DestinationId,
                cancellationToken);

            if (!destinationValidation.IsValid)
            {
                errors.Add($"Line {lineNum}: {destinationValidation.ErrorMessage}");
            }
        }

        return errors;
    }
}
