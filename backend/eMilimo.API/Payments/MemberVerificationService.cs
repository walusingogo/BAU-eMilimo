using System.Net;
using System.Security.Cryptography;
using eMilimo.API.Services;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Core.Services;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eMilimo.API.Payments;

public sealed class MemberVerificationNotifier : IMemberVerificationNotifier
{
    private readonly ICommunicationService _communications;
    private readonly IEmailService _email;
    private readonly ILogger<MemberVerificationNotifier> _logger;

    public MemberVerificationNotifier(
        ICommunicationService communications,
        IEmailService email,
        ILogger<MemberVerificationNotifier> logger)
    {
        _communications = communications;
        _email = email;
        _logger = logger;
    }

    public async Task<MemberVerificationDelivery> SendCodeAsync(
        string? phoneNumber,
        string? email,
        string memberName,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            try
            {
                await _communications.SendSmsAsync(
                    DigitalGivingSecurity.NormalizePhone(phoneNumber),
                    $"Your eMilimo giving verification code is {code}. It expires in 10 minutes. Do not share this code.",
                    cancellationToken,
                    "DigitalGivingOtp");
                return new MemberVerificationDelivery("SMS", DigitalGivingSecurity.MaskPhone(phoneNumber));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Digital Giving OTP SMS delivery failed; attempting the configured email fallback.");
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var safeName = WebUtility.HtmlEncode(memberName);
            var safeCode = WebUtility.HtmlEncode(code);
            await _email.SendAsync(
                email,
                "Your Digital Giving verification code",
                $"<p>Hi {safeName},</p><p>Your eMilimo Digital Giving verification code is:</p><p style=\"font-size:24px;font-weight:700;letter-spacing:4px\">{safeCode}</p><p>This code expires in 10 minutes. Do not share it with anyone.</p>",
                cancellationToken);
            return new MemberVerificationDelivery("EMAIL", DigitalGivingSecurity.MaskEmail(email));
        }

        throw new InvalidOperationException("The matching membership record has no verified delivery destination.");
    }
}

public sealed class MemberVerificationService : IMemberVerificationService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RequestCooldown = TimeSpan.FromSeconds(60);
    private const int MaximumAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IMemberVerificationNotifier _notifier;
    private readonly ILogger<MemberVerificationService> _logger;

    public MemberVerificationService(
        AppDbContext db,
        IMemberVerificationNotifier notifier,
        ILogger<MemberVerificationService> logger)
    {
        _db = db;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<MemberIdentificationResult> IdentifyAsync(
        string lookup,
        string? additionalIdentifier,
        string clientFingerprint,
        CancellationToken cancellationToken = default)
    {
        var normalizedLookup = DigitalGivingSecurity.NormalizeLookup(lookup ?? string.Empty);
        if (normalizedLookup.Length < 3)
            return new MemberIdentificationResult("NOT_FOUND", null, null, "Enter at least three characters from your membership number, registered phone number or full name.");
        var rawLookup = (lookup ?? string.Empty).Trim().ToUpperInvariant();
        var phoneDigits = new string(normalizedLookup.Where(char.IsDigit).ToArray());
        var localPhoneDigits = phoneDigits.StartsWith("260", StringComparison.Ordinal) && phoneDigits.Length > 3
            ? $"0{phoneDigits[3..]}"
            : phoneDigits;
        var candidates = await _db.Members.AsNoTracking()
            .Where(x => !x.IsArchived && x.IsActive && x.Status == MembershipStatuses.Active &&
                (x.MembershipNumber.ToUpper() == rawLookup ||
                 (phoneDigits.Length >= 7 && (x.NormalizedPhoneNumber == phoneDigits || x.NormalizedPhoneNumber == localPhoneDigits)) ||
                 (x.FirstName + " " + x.LastName).ToUpper() == rawLookup ||
                 (x.LastName + " " + x.FirstName).ToUpper() == rawLookup ||
                 (x.PreferredName != null && x.PreferredName.ToUpper() == rawLookup)))
            .Select(x => new { x.Id, x.MembershipNumber, x.NormalizedPhoneNumber, x.PhoneNumber, x.FirstName, x.MiddleNames, x.LastName })
            .Take(3)
            .ToListAsync(cancellationToken);

        if (candidates.Count > 1 && !string.IsNullOrWhiteSpace(additionalIdentifier))
        {
            var additional = DigitalGivingSecurity.NormalizeLookup(additionalIdentifier);
            var additionalDigits = new string(additional.Where(char.IsDigit).ToArray());
            var additionalLocal = additionalDigits.StartsWith("260", StringComparison.Ordinal) && additionalDigits.Length > 3
                ? $"0{additionalDigits[3..]}"
                : additionalDigits;
            var additionalRaw = additionalIdentifier.Trim().ToUpperInvariant();
            candidates = candidates.Where(x => x.MembershipNumber.ToUpperInvariant() == additionalRaw ||
                additionalDigits.Length >= 7 && (x.NormalizedPhoneNumber == additionalDigits || x.NormalizedPhoneNumber == additionalLocal)).ToList();
        }

        if (candidates.Count == 0)
            return new MemberIdentificationResult("NOT_FOUND", null, null, "We could not match an active membership record. Check the details or continue as a guest.");
        if (candidates.Count > 1)
            return new MemberIdentificationResult("MULTIPLE", null, null, "More than one active record matches that name. Enter your registered phone number or membership number as additional information.");

        var now = DateTime.UtcNow;
        var token = DigitalGivingSecurity.NewOpaqueToken();
        var challenge = new MemberVerificationChallenge
        {
            Id = Guid.NewGuid(),
            LookupHash = DigitalGivingSecurity.Hash(normalizedLookup),
            MemberId = candidates[0].Id,
            ClientFingerprintHash = DigitalGivingSecurity.Hash(clientFingerprint),
            OtpSalt = RandomNumberGenerator.GetBytes(16),
            OtpHash = RandomNumberGenerator.GetBytes(32),
            AttemptCount = 0,
            MaximumAttempts = 1,
            ExpiresAtUtc = now + Lifetime,
            DeliveryChannel = "DIRECT_MATCH",
            IsVerified = true,
            VerifiedAtUtc = now,
            VerificationTokenHash = DigitalGivingSecurity.Hash(token),
            CreatedDate = now,
            UpdatedDate = now,
            Version = 1
        };
        _db.MemberVerificationChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);
        var member = candidates[0];
        var fullName = string.Join(' ', new[] { member.FirstName, member.MiddleNames, member.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
        return new MemberIdentificationResult(
            "MATCHED",
            challenge.Id,
            token,
            "Your active membership record was matched. This does not sign you into an account.",
            fullName,
            member.MembershipNumber,
            member.PhoneNumber);
    }

    public async Task<MemberVerificationRequestResult> RequestAsync(
        string lookup,
        string clientFingerprint,
        CancellationToken cancellationToken = default)
    {
        var rawLookup = lookup?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedLookup = DigitalGivingSecurity.NormalizeLookup(lookup ?? string.Empty);
        var normalizedPhoneDigits = new string(normalizedLookup.Where(char.IsDigit).ToArray());
        var localPhoneDigits = normalizedPhoneDigits.StartsWith("260", StringComparison.Ordinal) && normalizedPhoneDigits.Length > 3
            ? $"0{normalizedPhoneDigits[3..]}"
            : normalizedPhoneDigits;
        var lookupHash = DigitalGivingSecurity.Hash(normalizedLookup);
        var fingerprintHash = DigitalGivingSecurity.Hash(clientFingerprint);
        var now = DateTime.UtcNow;
        var recent = await _db.MemberVerificationChallenges.AsNoTracking()
            .Where(x => x.LookupHash == lookupHash &&
                        x.ClientFingerprintHash == fingerprintHash &&
                        x.CreatedDate >= now - RequestCooldown &&
                        x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (recent is not null)
            return GenericResult(recent.Id, recent.ExpiresAtUtc);

        var member = await _db.Members.AsNoTracking().FirstOrDefaultAsync(x =>
            !x.IsArchived && x.IsActive && x.Status == MembershipStatuses.Active &&
            (x.MembershipNumber.ToUpper() == rawLookup ||
             x.NormalizedPhoneNumber == normalizedPhoneDigits ||
             x.NormalizedPhoneNumber == localPhoneDigits),
            cancellationToken);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var salt = RandomNumberGenerator.GetBytes(16);
        var challenge = new MemberVerificationChallenge
        {
            Id = Guid.NewGuid(),
            LookupHash = lookupHash,
            MemberId = member?.Id,
            ClientFingerprintHash = fingerprintHash,
            OtpSalt = salt,
            OtpHash = DigitalGivingSecurity.DeriveOtpHash(code, salt),
            AttemptCount = 0,
            MaximumAttempts = MaximumAttempts,
            ExpiresAtUtc = now + Lifetime,
            DeliveryChannel = "NONE",
            CreatedDate = now,
            UpdatedDate = now,
            Version = 1
        };
        _db.MemberVerificationChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        if (member is not null)
        {
            try
            {
                var displayName = member.PreferredName ?? $"{member.FirstName} {member.LastName}".Trim();
                var delivery = await _notifier.SendCodeAsync(
                    member.PhoneNumber,
                    member.Email,
                    displayName,
                    code,
                    cancellationToken);
                challenge.DeliveryChannel = delivery.Channel;
                challenge.DestinationHint = delivery.DestinationHint;
                challenge.UpdatedDate = DateTime.UtcNow;
                challenge.Version++;
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Member verification delivery could not be queued for challenge {ChallengeId}.", challenge.Id);
                challenge.DeliveryChannel = "FAILED";
                challenge.UpdatedDate = DateTime.UtcNow;
                challenge.Version++;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return GenericResult(challenge.Id, challenge.ExpiresAtUtc);
    }

    public async Task<MemberVerificationResult> VerifyAsync(
        Guid challengeId,
        string code,
        string clientFingerprint,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _db.MemberVerificationChallenges
            .Include(x => x.Member)
            .SingleOrDefaultAsync(x => x.Id == challengeId, cancellationToken);
        var genericFailure = new MemberVerificationResult(false, null, null, "The code is invalid or has expired.");
        if (challenge is null ||
            challenge.ClientFingerprintHash != DigitalGivingSecurity.Hash(clientFingerprint) ||
            challenge.ExpiresAtUtc <= DateTime.UtcNow ||
            challenge.ConsumedAtUtc.HasValue ||
            challenge.IsVerified ||
            challenge.AttemptCount >= challenge.MaximumAttempts)
            return genericFailure;

        challenge.AttemptCount++;
        challenge.UpdatedDate = DateTime.UtcNow;
        challenge.Version++;
        var actualHash = DigitalGivingSecurity.DeriveOtpHash(code?.Trim() ?? string.Empty, challenge.OtpSalt);
        if (challenge.Member is null || !DigitalGivingSecurity.FixedTimeEqualsBytes(challenge.OtpHash, actualHash))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return genericFailure;
        }

        var token = DigitalGivingSecurity.NewOpaqueToken();
        challenge.IsVerified = true;
        challenge.VerifiedAtUtc = DateTime.UtcNow;
        challenge.VerificationTokenHash = DigitalGivingSecurity.Hash(token);
        await _db.SaveChangesAsync(cancellationToken);
        var memberName = challenge.Member.PreferredName ?? $"{challenge.Member.FirstName} {challenge.Member.LastName}".Trim();
        return new MemberVerificationResult(true, token, memberName, "Membership verified.");
    }

    private static MemberVerificationRequestResult GenericResult(Guid challengeId, DateTime expiresAtUtc) => new(
        challengeId,
        expiresAtUtc,
        "If the membership details match an active record, a verification code has been sent to its registered contact.");
}
