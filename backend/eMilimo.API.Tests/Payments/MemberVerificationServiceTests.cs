using eMilimo.API.Payments;
using eMilimo.Core.Entities;
using eMilimo.Core.Payments;
using eMilimo.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eMilimo.API.Tests.Payments;

public sealed class MemberVerificationServiceTests
{
    [Fact]
    public async Task DirectIdentification_MatchesActiveMemberWithoutSendingOtpOrReturningIdentity()
    {
        await using var fixture = await VerificationFixture.CreateAsync(withMember: true);
        var notifier = new CaptureNotifier();
        var service = new MemberVerificationService(fixture.Db, notifier, NullLogger<MemberVerificationService>.Instance);

        var result = await service.IdentifyAsync("+260 977 000 000", null, "client-a");

        Assert.Equal("MATCHED", result.Status);
        Assert.NotNull(result.ChallengeId);
        Assert.NotNull(result.VerificationToken);
        Assert.Null(notifier.Code);
        Assert.Equal("Jane Banda", result.FullName);
        Assert.Equal("MEM-001", result.MembershipNumber);
        Assert.Equal("0977000000", result.PhoneNumber);
        Assert.DoesNotContain("Jane", result.Message, StringComparison.OrdinalIgnoreCase);
        var challenge = await fixture.Db.MemberVerificationChallenges.SingleAsync();
        Assert.True(challenge.IsVerified);
        Assert.NotNull(challenge.MemberId);
        Assert.Equal("DIRECT_MATCH", challenge.DeliveryChannel);
    }

    [Fact]
    public async Task DirectIdentification_RequiresAdditionalIdentifierForDuplicateName()
    {
        await using var fixture = await VerificationFixture.CreateAsync(withMember: true);
        fixture.Db.Members.Add(new Member
        {
            Id = Guid.NewGuid(),
            MembershipNumber = "MEM-002",
            FirstName = "Jane",
            LastName = "Banda",
            PreferredName = "Jane Banda",
            PhoneNumber = "0966000000",
            NormalizedPhoneNumber = "0966000000",
            NormalizedSearchName = "JANE BANDA",
            Status = MembershipStatuses.Active,
            IsActive = true
        });
        await fixture.Db.SaveChangesAsync();
        var service = new MemberVerificationService(fixture.Db, new CaptureNotifier(), NullLogger<MemberVerificationService>.Instance);

        var ambiguous = await service.IdentifyAsync("Jane Banda", null, "client-a");
        var refined = await service.IdentifyAsync("Jane Banda", "MEM-002", "client-a");

        Assert.Equal("MULTIPLE", ambiguous.Status);
        Assert.Null(ambiguous.ChallengeId);
        Assert.Equal("MATCHED", refined.Status);
        Assert.Single(await fixture.Db.MemberVerificationChallenges.ToListAsync());
    }

    [Fact]
    public async Task DirectIdentification_UnknownMemberDoesNotCreateChallenge()
    {
        await using var fixture = await VerificationFixture.CreateAsync(withMember: false);
        var service = new MemberVerificationService(fixture.Db, new CaptureNotifier(), NullLogger<MemberVerificationService>.Instance);

        var result = await service.IdentifyAsync("Unknown Person", null, "client-a");

        Assert.Equal("NOT_FOUND", result.Status);
        Assert.Null(result.ChallengeId);
        Assert.Empty(await fixture.Db.MemberVerificationChallenges.ToListAsync());
    }

    [Fact]
    public async Task RegisteredMember_CanVerifyWithDeliveredOtp()
    {
        await using var fixture = await VerificationFixture.CreateAsync(withMember: true);
        var notifier = new CaptureNotifier();
        var service = new MemberVerificationService(fixture.Db, notifier, NullLogger<MemberVerificationService>.Instance);

        var requested = await service.RequestAsync("0977000000", "client-a");
        var wrongCode = notifier.Code == "000000" ? "999999" : "000000";
        var incorrect = await service.VerifyAsync(requested.ChallengeId, wrongCode, "client-a");
        var verified = await service.VerifyAsync(requested.ChallengeId, notifier.Code!, "client-a");

        Assert.False(incorrect.Verified);
        Assert.True(verified.Verified);
        Assert.NotNull(verified.VerificationToken);
        Assert.Equal("Jane Banda", verified.MemberDisplayName);
        var challenge = await fixture.Db.MemberVerificationChallenges.SingleAsync();
        Assert.True(challenge.IsVerified);
        Assert.Equal(2, challenge.AttemptCount);
        Assert.NotEqual(notifier.Code, Convert.ToHexString(challenge.OtpHash));
    }

    [Fact]
    public async Task UnknownMember_ReturnsSameGenericResponseAndDoesNotSendOtp()
    {
        await using var fixture = await VerificationFixture.CreateAsync(withMember: false);
        var notifier = new CaptureNotifier();
        var service = new MemberVerificationService(fixture.Db, notifier, NullLogger<MemberVerificationService>.Instance);

        var requested = await service.RequestAsync("UNKNOWN-100", "client-a");
        var verification = await service.VerifyAsync(requested.ChallengeId, "123456", "client-a");

        Assert.Contains("If the membership details match", requested.Message);
        Assert.False(verification.Verified);
        Assert.Null(notifier.Code);
        var challenge = await fixture.Db.MemberVerificationChallenges.SingleAsync();
        Assert.Null(challenge.MemberId);
    }

    [Fact]
    public async Task Challenge_CannotBeVerifiedFromDifferentClientFingerprint()
    {
        await using var fixture = await VerificationFixture.CreateAsync(withMember: true);
        var notifier = new CaptureNotifier();
        var service = new MemberVerificationService(fixture.Db, notifier, NullLogger<MemberVerificationService>.Instance);

        var requested = await service.RequestAsync("MEM-001", "client-a");
        var verification = await service.VerifyAsync(requested.ChallengeId, notifier.Code!, "client-b");

        Assert.False(verification.Verified);
        Assert.Equal(0, (await fixture.Db.MemberVerificationChallenges.SingleAsync()).AttemptCount);
    }

    private sealed class CaptureNotifier : IMemberVerificationNotifier
    {
        public string? Code { get; private set; }

        public Task<MemberVerificationDelivery> SendCodeAsync(
            string? phoneNumber,
            string? email,
            string memberName,
            string code,
            CancellationToken cancellationToken = default)
        {
            Code = code;
            return Task.FromResult(new MemberVerificationDelivery("SMS", "+260***000"));
        }
    }

    private sealed class VerificationFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private VerificationFixture(SqliteConnection connection, AppDbContext db) { _connection = connection; Db = db; }
        public AppDbContext Db { get; }

        public static async Task<VerificationFixture> CreateAsync(bool withMember)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection).UseSnakeCaseNamingConvention().Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            if (withMember)
            {
                db.Members.Add(new Member
                {
                    Id = Guid.NewGuid(),
                    MembershipNumber = "MEM-001",
                    FirstName = "Jane",
                    LastName = "Banda",
                    PreferredName = "Jane Banda",
                    PhoneNumber = "0977000000",
                    NormalizedPhoneNumber = "0977000000",
                    NormalizedSearchName = "JANE BANDA",
                    Status = MembershipStatuses.Active,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
            return new VerificationFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
