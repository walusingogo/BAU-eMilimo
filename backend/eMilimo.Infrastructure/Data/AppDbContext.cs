// backend/eMilimo.Infrastructure/Data/AppDbContext.cs
using eMilimo.Core.Entities.Companions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using eMilimo.Core.Entities;
using BAU.Platform.Outbox.EntityFrameworkCore;

namespace eMilimo.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Requisition> Requisitions { get; set; }
        public DbSet<RequisitionAtm> RequisitionAtms { get; set; }
        public DbSet<RequisitionItem> RequisitionItems { get; set; }
        public DbSet<ContributionType> ContributionTypes { get; set; }
        public DbSet<ContributionTypeDestinationPolicy> ContributionTypeDestinationPolicies { get; set; }
        public DbSet<ContributionSource> ContributionSources { get; set; }
        public DbSet<Contribution> Contributions { get; set; }
        public DbSet<ContributionAud> ContributionAudits { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<AssetCheckout> AssetCheckouts { get; set; }
        public DbSet<AssetMovement> AssetMovements { get; set; }
        public DbSet<AssetAtm> AssetAtms { get; set; }
        public DbSet<AssetCheckoutAtm> AssetCheckoutAtms { get; set; }
        public DbSet<AssetLck> AssetLcks { get; set; }
        public DbSet<AssetAud> AssetAudits { get; set; }
        public DbSet<RequisitionAud> RequisitionAudits { get; set; }
        public DbSet<ContributionAllocation> ContributionAllocations { get; set; }
        public DbSet<ContributionDenomination> ContributionDenominations { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<FundBalance> FundBalances { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }
        public DbSet<AssetDriver> AssetDrivers { get; set; }
        public DbSet<FundTransaction> FundTransactions { get; set; }
        public DbSet<ContributionAtm> ContributionAtms { get; set; }
        public DbSet<ContributionLck> ContributionLcks { get; set; }
        public DbSet<UserGroupAssignment> UserGroupAssignments { get; set; }
        public DbSet<Committee> Committees { get; set; }
        public DbSet<UserCommitteeAssignment> UserCommitteeAssignments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<FunctionalArea> FunctionalAreas { get; set; }
        public DbSet<OfficeAppointment> OfficeAppointments { get; set; }
        public DbSet<SystemLog> Logs { get; set; }
        public DbSet<RequisitionLck> RequisitionLcks { get; set; }
        public DbSet<UserLck> UserLcks { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<SmtpSetting> SmtpSettings { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<CommunicationLog> CommunicationLogs { get; set; }
        public DbSet<SystemLicense> SystemLicenses { get; set; }
        public DbSet<LicenseTrustedKey> LicenseTrustedKeys { get; set; }
        public DbSet<LicenseKeyRotationAud> LicenseKeyRotationAudits { get; set; }
        public DbSet<IntegrationSetting> IntegrationSettings { get; set; }
        public DbSet<ReferenceNumberSequence> ReferenceNumberSequences { get; set; }
        public DbSet<ReferenceNumberConfiguration> ReferenceNumberConfigurations { get; set; }
        public DbSet<FinancialRecordBook> FinancialRecordBooks { get; set; }
        public DbSet<FinancialRecordBookContribution> FinancialRecordBookContributions { get; set; }
        public DbSet<FrbAssessmentRule> FrbAssessmentRules { get; set; }
        public DbSet<FrbAssessmentRuleContributionType> FrbAssessmentRuleContributionTypes { get; set; }
        public DbSet<FrbAssessmentAllocationLine> FrbAssessmentAllocationLines { get; set; }
        public DbSet<FrbAssessmentSnapshot> FrbAssessmentSnapshots { get; set; }
        public DbSet<FrbAssessmentAllocationSnapshot> FrbAssessmentAllocationSnapshots { get; set; }
        public DbSet<FrbVerification> FrbVerifications { get; set; }
        public DbSet<FrbAud> FrbAudits { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<FcyManagementSetting> FcyManagementSettings { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MembershipStatusHistory> MembershipStatusHistory { get; set; }
        public DbSet<MemberExternalIdentity> MemberExternalIdentities { get; set; }
        public DbSet<MembershipIntegrationConfiguration> MembershipIntegrationConfigurations { get; set; }
        public DbSet<MembershipSyncLog> MembershipSyncLogs { get; set; }
        public DbSet<MemberAud> MemberAudits { get; set; }
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<ServiceEvent> ServiceEvents { get; set; }
        public DbSet<PaymentMethodDefinition> PaymentMethodDefinitions { get; set; }
        public DbSet<ServiceCollection> ServiceCollections { get; set; }
        public DbSet<ServiceCollectionEntry> ServiceCollectionEntries { get; set; }
        public DbSet<ServiceCollectionEntryLine> ServiceCollectionEntryLines { get; set; }
        public DbSet<ServiceCollectionDenomination> ServiceCollectionDenominations { get; set; }
        public DbSet<ServiceCollectionTender> ServiceCollectionTenders { get; set; }
        public DbSet<ServiceCollectionParticipant> ServiceCollectionParticipants { get; set; }
        public DbSet<ServiceCollectionAtm> ServiceCollectionAttachments { get; set; }
        public DbSet<ServiceCollectionLck> ServiceCollectionLocks { get; set; }
        public DbSet<ServiceCollectionAud> ServiceCollectionAudits { get; set; }
        public DbSet<FinancialRecordBookServiceCollection> FinancialRecordBookServiceCollections { get; set; }
        public DbSet<DigitalGivingConfiguration> DigitalGivingConfigurations { get; set; }
        public DbSet<DigitalGivingTypeSetting> DigitalGivingTypeSettings { get; set; }
        public DbSet<PaymentProviderConfiguration> PaymentProviderConfigurations { get; set; }
        public DbSet<IntegrationConfigurationAud> IntegrationConfigurationAudits { get; set; }
        public DbSet<PaymentProviderRoute> PaymentProviderRoutes { get; set; }
        public DbSet<DigitalCollection> DigitalCollections { get; set; }
        public DbSet<DigitalCollectionItem> DigitalCollectionItems { get; set; }
        public DbSet<DigitalCollectionEvent> DigitalCollectionEvents { get; set; }
        public DbSet<DigitalCollectionAtm> DigitalCollectionAttachments { get; set; }
        public DbSet<DigitalCollectionLck> DigitalCollectionLocks { get; set; }
        public DbSet<DigitalCollectionAud> DigitalCollectionAudits { get; set; }
        public DbSet<DigitalCollectionItemAtm> DigitalCollectionItemAttachments { get; set; }
        public DbSet<DigitalCollectionItemLck> DigitalCollectionItemLocks { get; set; }
        public DbSet<DigitalCollectionItemAud> DigitalCollectionItemAudits { get; set; }
        public DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; set; }
        public DbSet<PaymentApiExchange> PaymentApiExchanges { get; set; }
        public DbSet<MtnSandboxScenario> MtnSandboxScenarios { get; set; }
        public DbSet<MtnSandboxTestExecution> MtnSandboxTestExecutions { get; set; }
        public DbSet<MemberVerificationChallenge> MemberVerificationChallenges { get; set; }
        public DbSet<FinancialAdjustment> FinancialAdjustments { get; set; }
        public DbSet<OrganizationOverview> OrganizationOverviews { get; set; }
        public DbSet<OrganizationContributionTypeSummary> OrganizationContributionTypeSummaries { get; set; }
        public DbSet<AccountingIntegrationProfile> AccountingIntegrationProfiles { get; set; }
        public DbSet<AccountingAccountMapping> AccountingAccountMappings { get; set; }
        public DbSet<AccountingExternalCode> AccountingExternalCodes { get; set; }
        public DbSet<AccountingBatch> AccountingBatches { get; set; }
        public DbSet<AccountingBatchLine> AccountingBatchLines { get; set; }
        public DbSet<AccountingIntegrationProfileAtm> AccountingIntegrationProfileAttachments { get; set; }
        public DbSet<AccountingIntegrationProfileLck> AccountingIntegrationProfileLocks { get; set; }
        public DbSet<AccountingIntegrationProfileAud> AccountingIntegrationProfileAudits { get; set; }
        public DbSet<AccountingAccountMappingAtm> AccountingAccountMappingAttachments { get; set; }
        public DbSet<AccountingAccountMappingLck> AccountingAccountMappingLocks { get; set; }
        public DbSet<AccountingAccountMappingAud> AccountingAccountMappingAudits { get; set; }
        public DbSet<AccountingExternalCodeAtm> AccountingExternalCodeAttachments { get; set; }
        public DbSet<AccountingExternalCodeLck> AccountingExternalCodeLocks { get; set; }
        public DbSet<AccountingExternalCodeAud> AccountingExternalCodeAudits { get; set; }
        public DbSet<AccountingBatchAtm> AccountingBatchAttachments { get; set; }
        public DbSet<AccountingBatchLck> AccountingBatchLocks { get; set; }
        public DbSet<AccountingBatchAud> AccountingBatchAudits { get; set; }
        public DbSet<AccountingBatchLineAtm> AccountingBatchLineAttachments { get; set; }
        public DbSet<AccountingBatchLineLck> AccountingBatchLineLocks { get; set; }
        public DbSet<AccountingBatchLineAud> AccountingBatchLineAudits { get; set; }
        public DbSet<SectionFinancialSummary> SectionFinancialSummaries { get; set; }
        public DbSet<GroupFinancialSummary> GroupFinancialSummaries { get; set; }
        public DbSet<CommitteeFinancialSummary> CommitteeFinancialSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddBauOutbox();

            modelBuilder.Entity<DigitalGivingConfiguration>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.PublicSlug).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ContributionSource).WithMany()
                    .HasForeignKey(x => x.ContributionSourceId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_digital_giving_configuration_minimum", "minimum_amount > 0");
                    t.HasCheckConstraint("ck_digital_giving_configuration_maximum", "maximum_amount IS NULL OR maximum_amount >= minimum_amount");
                });
            });

            modelBuilder.Entity<DigitalGivingTypeSetting>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DigitalGivingConfigurationId, x.ContributionTypeId }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.DigitalGivingConfiguration).WithMany(x => x.ContributionTypes)
                    .HasForeignKey(x => x.DigitalGivingConfigurationId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ContributionType).WithMany()
                    .HasForeignKey(x => x.ContributionTypeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Committee).WithMany().HasForeignKey(x => x.CommitteeId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_digital_giving_type_destination_updated",
                    "((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1"));
            });

            modelBuilder.Entity<PaymentProviderConfiguration>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ProviderKey, x.Environment }).IsUnique();
                entity.Property(x => x.ProtectedConfiguration).HasColumnType("text");
                entity.Property(x => x.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<IntegrationConfigurationAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.IntegrationConfigurationId, x.ConfigurationVersion }).IsUnique();
                entity.HasIndex(x => new { x.IntegrationConfigurationId, x.CreatedDate });
                entity.Property(x => x.ProtectedConfigurationSnapshot).HasColumnType("text");
                entity.Property(x => x.SanitizedConfigurationSnapshot).HasColumnType("jsonb");
                entity.Property(x => x.ChangedFieldsJson).HasColumnType("jsonb");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.PaymentProviderConfiguration).WithMany()
                    .HasForeignKey(x => x.IntegrationConfigurationId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("ck_provider_configuration_version_positive", "configuration_version > 0"));
            });

            modelBuilder.Entity<PaymentProviderRoute>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.PaymentMethodDefinitionId, x.PaymentProviderConfigurationId }).IsUnique();
                entity.HasIndex(x => new { x.PaymentMethodDefinitionId, x.Priority }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.PaymentMethodDefinition).WithMany()
                    .HasForeignKey(x => x.PaymentMethodDefinitionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.PaymentProviderConfiguration).WithMany(x => x.Routes)
                    .HasForeignKey(x => x.PaymentProviderConfigurationId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("ck_payment_provider_route_priority", "priority >= 0"));
            });

            modelBuilder.Entity<DigitalCollection>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.PaymentReference).IsUnique();
                entity.HasIndex(x => x.IdempotencyKey).IsUnique();
                entity.HasIndex(x => x.PublicStatusTokenHash).IsUnique();
                entity.HasIndex(x => new { x.PaymentProviderConfigurationId, x.ProviderTransactionId }).IsUnique()
                    .HasFilter("provider_transaction_id IS NOT NULL");
                entity.HasIndex(x => new { x.Status, x.CreatedDate });
                entity.HasIndex(x => new { x.SettlementStatus, x.CreatedDate });
                entity.HasIndex(x => new { x.ReconciliationStatus, x.NextReconciliationAtUtc });
                entity.Property(x => x.ProtectedPayerData).HasColumnType("text");
                entity.Property(x => x.ProtectedPublicStatusToken).HasColumnType("text");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.PaymentMethodDefinition).WithMany()
                    .HasForeignKey(x => x.PaymentMethodDefinitionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ContributionSource).WithMany()
                    .HasForeignKey(x => x.ContributionSourceId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.PaymentProviderConfiguration).WithMany()
                    .HasForeignKey(x => x.PaymentProviderConfigurationId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_digital_collection_amounts",
                    "gross_amount > 0 AND provider_fee >= 0 AND net_settlement_amount >= 0 AND provider_fee <= gross_amount"));
            });

            modelBuilder.Entity<DigitalCollectionItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.DigitalCollectionId);
                entity.HasIndex(x => x.ContributionId).IsUnique().HasFilter("contribution_id IS NOT NULL");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.DigitalCollection).WithMany(x => x.Lines)
                    .HasForeignKey(x => x.DigitalCollectionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ContributionType).WithMany()
                    .HasForeignKey(x => x.ContributionTypeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Committee).WithMany().HasForeignKey(x => x.CommitteeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Contribution).WithMany().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_digital_collection_item_amount", "amount > 0");
                    t.HasCheckConstraint(
                        "ck_digital_collection_item_destination",
                        "((CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1");
                });
            });

            modelBuilder.Entity<DigitalCollectionAtm>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.EntityId);
                entity.HasOne<DigitalCollection>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DigitalCollectionLck>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.EntityId, x.ReleasedAtUtc });
                entity.HasOne<DigitalCollection>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.LockedByUser).WithMany().HasForeignKey(x => x.LockedById).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<DigitalCollectionAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DigitalCollectionId, x.PerformedAtUtc });
                entity.Property(x => x.ChangesJson).HasColumnType("jsonb");
                entity.HasOne(x => x.DigitalCollection).WithMany().HasForeignKey(x => x.DigitalCollectionId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DigitalCollectionItemAtm>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.EntityId);
                entity.HasOne<DigitalCollectionItem>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DigitalCollectionItemLck>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.EntityId, x.ReleasedAtUtc });
                entity.HasOne<DigitalCollectionItem>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.LockedByUser).WithMany().HasForeignKey(x => x.LockedById).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<DigitalCollectionItemAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DigitalCollectionItemId, x.PerformedAtUtc });
                entity.Property(x => x.ChangesJson).HasColumnType("jsonb");
                entity.HasOne(x => x.DigitalCollectionItem).WithMany().HasForeignKey(x => x.DigitalCollectionItemId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DigitalCollectionEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DigitalCollectionId, x.OccurredAtUtc });
                entity.Property(x => x.ProtectedDetails).HasColumnType("text");
                entity.Property(x => x.SanitizedDetailsJson).HasColumnType("jsonb");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.DigitalCollection).WithMany(x => x.Events)
                    .HasForeignKey(x => x.DigitalCollectionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FinancialAdjustment>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DigitalCollectionId, x.Status });
                entity.HasIndex(x => x.RelatedAdjustmentId);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.DigitalCollection).WithMany()
                    .HasForeignKey(x => x.DigitalCollectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.RelatedAdjustment).WithMany()
                    .HasForeignKey(x => x.RelatedAdjustmentId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(x => x.CompletedBy).WithMany()
                    .HasForeignKey(x => x.CompletedById).OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_financial_adjustment_type", "adjustment_type IN ('ACCOUNTING_REVERSAL')");
                    t.HasCheckConstraint("ck_financial_adjustment_status", "status IN ('REQUESTED','APPROVED','REJECTED')");
                });
            });

            modelBuilder.Entity<PaymentWebhookEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.PaymentProviderConfigurationId, x.EventKey }).IsUnique();
                entity.HasIndex(x => x.PayloadHash);
                entity.HasIndex(x => new { x.ProcessingStatus, x.ReceivedAtUtc });
                entity.Property(x => x.ProtectedPayload).HasColumnType("text");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.PaymentProviderConfiguration).WithMany()
                    .HasForeignKey(x => x.PaymentProviderConfigurationId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.DigitalCollection).WithMany()
                    .HasForeignKey(x => x.DigitalCollectionId).OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t => t.HasCheckConstraint("ck_payment_webhook_processing_attempts", "processing_attempts >= 0"));
            });

            modelBuilder.Entity<PaymentApiExchange>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.PaymentProviderConfigurationId, x.RequestedAtUtc });
                entity.HasIndex(x => new { x.DigitalCollectionId, x.RequestedAtUtc });
                entity.HasIndex(x => new { x.HttpStatusCode, x.RequestedAtUtc });
                entity.HasIndex(x => new { x.Environment, x.RequestedAtUtc });
                entity.HasIndex(x => x.TransactionReference);
                entity.Property(x => x.SanitizedRequestHeaders).HasColumnType("jsonb");
                entity.Property(x => x.SanitizedResponseHeaders).HasColumnType("jsonb");
                entity.Property(x => x.SanitizedRequestPayload).HasColumnType("jsonb");
                entity.Property(x => x.SanitizedResponsePayload).HasColumnType("jsonb");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.PaymentProviderConfiguration).WithMany()
                    .HasForeignKey(x => x.PaymentProviderConfigurationId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.DigitalCollection).WithMany()
                    .HasForeignKey(x => x.DigitalCollectionId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(x => x.MtnSandboxTestExecution).WithMany(x => x.ApiExchanges)
                    .HasForeignKey(x => x.MtnSandboxTestExecutionId).OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t => t.HasCheckConstraint("ck_payment_api_exchange_duration", "duration_milliseconds >= 0 AND attempt_number > 0"));
            });

            modelBuilder.Entity<MtnSandboxScenario>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.ScenarioCode).IsUnique();
                entity.HasIndex(x => new { x.IsActive, x.Name });
                entity.Property(x => x.SamplePresetsJson).HasColumnType("jsonb");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_mtn_sandbox_scenario_amount", "test_amount > 0");
                    t.HasCheckConstraint("ck_mtn_sandbox_scenario_polling", "maximum_polling_attempts > 0 AND polling_interval_seconds > 0");
                    t.HasCheckConstraint("ck_mtn_sandbox_scenario_configuration_version", "configuration_version > 0");
                });
            });

            modelBuilder.Entity<MtnSandboxTestExecution>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.TransactionUuid).IsUnique();
                entity.HasIndex(x => x.ExternalReference).IsUnique();
                entity.HasIndex(x => new { x.PaymentProviderConfigurationId, x.StartedAtUtc });
                entity.HasIndex(x => new { x.MtnSandboxScenarioId, x.StartedAtUtc });
                entity.HasIndex(x => new { x.ExecutionStatus, x.NextPollAtUtc });
                entity.Property(x => x.SanitizedRequestPreview).HasColumnType("jsonb");
                entity.Property(x => x.TimelineJson).HasColumnType("jsonb");
                entity.Property(x => x.SamplePresetSnapshotJson).HasColumnType("jsonb");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.PaymentProviderConfiguration).WithMany()
                    .HasForeignKey(x => x.PaymentProviderConfigurationId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.MtnSandboxScenario).WithMany(x => x.Executions)
                    .HasForeignKey(x => x.MtnSandboxScenarioId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_mtn_sandbox_execution_amount", "amount > 0");
                    t.HasCheckConstraint("ck_mtn_sandbox_execution_polling", "polling_attempt_count >= 0 AND maximum_polling_attempts > 0 AND polling_interval_seconds > 0");
                });
            });

            modelBuilder.Entity<MemberVerificationChallenge>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.LookupHash, x.CreatedDate });
                entity.HasIndex(x => x.ExpiresAtUtc);
                entity.HasIndex(x => x.VerificationTokenHash)
                    .HasFilter("verification_token_hash IS NOT NULL");
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_member_verification_attempt_count", "attempt_count >= 0 AND maximum_attempts > 0");
                    t.HasCheckConstraint("ck_member_verification_otp_material", "length(otp_salt) >= 16 AND length(otp_hash) >= 32");
                });
            });

            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(3);
                entity.Property(x => x.Name).HasMaxLength(100);
                entity.Property(x => x.Symbol).HasMaxLength(10);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.ToTable(t => t.HasCheckConstraint("ck_currencies_decimal_places", "decimal_places BETWEEN 0 AND 6"));
            });
            modelBuilder.Entity<FcyManagementSetting>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.BaseCurrencyCode).HasMaxLength(3);
                entity.Property(x => x.RateSourceMode).HasMaxLength(40);
                entity.Property(x => x.ProviderCode).HasMaxLength(100);
                entity.Property(x => x.OverrideTolerancePercent).HasPrecision(9, 4);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.BaseCurrencyCode)
                    .HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_fcy_settings_rate_age", "maximum_rate_age_hours BETWEEN 1 AND 8760");
                    t.HasCheckConstraint("ck_fcy_settings_override_tolerance", "override_tolerance_percent BETWEEN 0 AND 100");
                });
            });
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FromCurrencyCode, x.ToCurrencyCode, x.EffectiveAtUtc }).IsUnique();
                entity.HasIndex(x => new { x.FromCurrencyCode, x.ToCurrencyCode, x.IsApproved, x.EffectiveAtUtc });
                entity.Property(x => x.FromCurrencyCode).HasMaxLength(3);
                entity.Property(x => x.ToCurrencyCode).HasMaxLength(3);
                entity.Property(x => x.Rate).HasPrecision(28, 12);
                entity.Property(x => x.SourceKind).HasMaxLength(20);
                entity.Property(x => x.SourceReference).HasMaxLength(200);
                entity.Property(x => x.Reason).HasMaxLength(1000);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.FromCurrencyCode)
                    .HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.ToCurrencyCode)
                    .HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_exchange_rates_positive", "rate > 0");
                    t.HasCheckConstraint("ck_exchange_rates_pair", "from_currency_code <> to_currency_code");
                    t.HasCheckConstraint("ck_exchange_rates_dates", "expires_at_utc IS NULL OR expires_at_utc > effective_at_utc");
                });
            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.MembershipNumber).IsUnique();
                entity.HasIndex(x => x.NormalizedSearchName);
                entity.HasIndex(x => x.NormalizedPhoneNumber);
                entity.HasIndex(x => new { x.Status, x.IsArchived });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.SetNull);
            });
            modelBuilder.Entity<MembershipStatusHistory>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.MemberId, x.EffectiveAtUtc });
                entity.HasOne(x => x.Member).WithMany(x => x.StatusHistory).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedById).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<MemberExternalIdentity>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ProviderCode, x.ExternalMemberId }).IsUnique();
                entity.HasOne(x => x.Member).WithMany(x => x.ExternalIdentities).HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<MembershipIntegrationConfiguration>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.ProviderCode).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
            });
            modelBuilder.Entity<MembershipSyncLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ProviderCode, x.StartedAtUtc });
            });
            modelBuilder.Entity<MemberAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.MemberId, x.PerformedAtUtc });
                entity.Property(x => x.ChangesJson).HasColumnType("text");
            });
            modelBuilder.Entity<ServiceType>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
            });
            modelBuilder.Entity<ServiceEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.StartsAtUtc, x.ServiceTypeId });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ServiceType).WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Committee).WithMany().HasForeignKey(x => x.CommitteeId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<PaymentMethodDefinition>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Code).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
            });
            modelBuilder.Entity<ServiceCollection>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.CollectionNumber).IsUnique();
                entity.HasIndex(x => new { x.Status, x.CreatedDate });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ServiceEvent).WithMany().HasForeignKey(x => x.ServiceEventId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ContributionSource).WithMany().HasForeignKey(x => x.ContributionSourceId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Committee).WithMany().HasForeignKey(x => x.CommitteeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<FinancialRecordBook>().WithMany().HasForeignKey(x => x.FinalizedFinancialRecordBookId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("ck_service_collections_scope", "((CASE WHEN section_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN group_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN committee_id IS NOT NULL THEN 1 ELSE 0 END)) <= 1"));
            });
            modelBuilder.Entity<ServiceCollectionEntry>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ServiceCollectionId, x.MemberId });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ServiceCollection).WithMany(x => x.Entries).HasForeignKey(x => x.ServiceCollectionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ServiceCollectionEntryLine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ServiceCollectionEntryId, x.ContributionTypeId });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ServiceCollectionEntry).WithMany(x => x.Lines).HasForeignKey(x => x.ServiceCollectionEntryId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ContributionType).WithMany().HasForeignKey(x => x.ContributionTypeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(x => x.ContributionId).IsUnique().HasFilter("contribution_id IS NOT NULL");
                entity.HasOne(x => x.Contribution).WithOne(x => x.ServiceCollectionEntryLine)
                    .HasForeignKey<ServiceCollectionEntryLine>(x => x.ContributionId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("ck_service_collection_entry_lines_amount", "amount > 0"));
            });
            modelBuilder.Entity<ServiceCollectionDenomination>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ServiceCollectionId, x.CurrencyCode, x.Denomination }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ServiceCollection).WithMany(x => x.Denominations).HasForeignKey(x => x.ServiceCollectionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ExchangeRate>().WithMany().HasForeignKey(x => x.ExchangeRateId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ServiceCollectionTender>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.ServiceCollection).WithMany(x => x.Tenders).HasForeignKey(x => x.ServiceCollectionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.PaymentMethodDefinition).WithMany().HasForeignKey(x => x.PaymentMethodDefinitionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyCode).HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ExchangeRate>().WithMany().HasForeignKey(x => x.ExchangeRateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ServiceCollectionParticipant>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ServiceCollectionId, x.UserId, x.Responsibility }).IsUnique();
                entity.HasOne(x => x.ServiceCollection).WithMany(x => x.Participants).HasForeignKey(x => x.ServiceCollectionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<ServiceCollectionAtm>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.EntityId);
                entity.HasOne<ServiceCollection>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ServiceCollectionLck>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.EntityId, x.ReleasedAtUtc });
                entity.HasOne<ServiceCollection>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ServiceCollectionAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ServiceCollectionId, x.PerformedAtUtc });
                entity.Property(x => x.ChangesJson).HasColumnType("text");
            });
            modelBuilder.Entity<FinancialRecordBookServiceCollection>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FinancialRecordBookId, x.ServiceCollectionId }).IsUnique();
                entity.HasIndex(x => x.ServiceCollectionId).IsUnique();
                entity.HasOne(x => x.FinancialRecordBook).WithMany(x => x.ServiceCollections).HasForeignKey(x => x.FinancialRecordBookId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ServiceCollection).WithMany().HasForeignKey(x => x.ServiceCollectionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReferenceNumberSequence>(entity =>
            {
                entity.HasKey(x => new { x.ReferenceType, x.SequenceDate });
                entity.Property(x => x.ReferenceType).HasMaxLength(30);
            });
            modelBuilder.Entity<ReferenceNumberConfiguration>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ReferenceType, x.EffectiveFrom }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
            });
            modelBuilder.Entity<FinancialRecordBook>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.FrbNumber).IsUnique();
                entity.HasIndex(x => new { x.Status, x.PeriodStart, x.PeriodEnd });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<FinancialRecordBookContribution>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FinancialRecordBookId, x.ContributionId }).IsUnique();
                entity.HasOne(x => x.FinancialRecordBook).WithMany(x => x.Contributions).HasForeignKey(x => x.FinancialRecordBookId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Contribution).WithMany().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<FrbAssessmentRule>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.Name, x.EffectiveFrom }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
            });
            modelBuilder.Entity<FrbAssessmentRuleContributionType>(entity =>
            {
                entity.HasKey(x => new { x.FrbAssessmentRuleId, x.ContributionTypeId });
                entity.HasOne(x => x.FrbAssessmentRule).WithMany(x => x.ContributionTypes).HasForeignKey(x => x.FrbAssessmentRuleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ContributionType).WithMany().HasForeignKey(x => x.ContributionTypeId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<FrbAssessmentAllocationLine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FrbAssessmentRuleId, x.Beneficiary }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.FrbAssessmentRule).WithMany(x => x.AllocationLines).HasForeignKey(x => x.FrbAssessmentRuleId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<FrbAssessmentSnapshot>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FinancialRecordBookId, x.SourceRuleId }).IsUnique();
                entity.HasOne(x => x.FinancialRecordBook).WithMany(x => x.AssessmentSnapshots).HasForeignKey(x => x.FinancialRecordBookId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<FrbAssessmentRule>().WithMany().HasForeignKey(x => x.SourceRuleId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<FrbAssessmentAllocationSnapshot>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FrbAssessmentSnapshotId, x.DisplayOrder });
                entity.HasOne(x => x.FrbAssessmentSnapshot).WithMany(x => x.AllocationLines).HasForeignKey(x => x.FrbAssessmentSnapshotId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<FrbVerification>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FinancialRecordBookId, x.OfficerType }).IsUnique();
                entity.HasOne(x => x.FinancialRecordBook).WithMany(x => x.Verifications).HasForeignKey(x => x.FinancialRecordBookId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<User>().WithMany().HasForeignKey(x => x.VerifiedById).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<FrbAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FinancialRecordBookId, x.PerformedAtUtc });
                entity.Property(x => x.ChangesJson).HasColumnType("text");
                entity.HasOne<FinancialRecordBook>().WithMany().HasForeignKey(x => x.FinancialRecordBookId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CommunicationLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.SentAtUtc);
                entity.HasIndex(x => new { x.Channel, x.Status, x.SentAtUtc });
                entity.HasIndex(x => x.RetryOfCommunicationLogId);
                entity.Property(x => x.ProtectedMessage).HasColumnType("text");
                entity.Property(x => x.ProtectedCcRecipients).HasColumnType("text");
            });

            // ---------- USER ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.MembershipId).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.Property(e => e.ProfilePhoto)
                    .HasColumnName("profile_photo");

                entity.Property(e => e.SignatureData)
                    .HasColumnName("signature_data");

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Section)
                    .WithMany(s => s.Users)
                    .HasForeignKey(e => e.SectionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Group)
                    .WithMany(g => g.Users)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- SECTION ----------
            modelBuilder.Entity<Section>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SectionNumber).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            // ---------- GROUP ----------
            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.GroupName).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(g => g.Leader)
                    .WithMany()
                    .HasForeignKey(g => g.LeaderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(g => g.ParentGroup)
                    .WithMany(g => g.ChildGroups)
                    .HasForeignKey(g => g.ParentGroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(g => g.Section)
                    .WithMany()
                    .HasForeignKey(g => g.SectionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserGroupAssignment>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.GroupId });
                entity.HasOne(e => e.User)
                    .WithMany(u => u.GroupAssignments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Group)
                    .WithMany(g => g.UserAssignments)
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Committee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CommitteeName).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.HasOne(e => e.ParentSection).WithMany().HasForeignKey(e => e.ParentSectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ParentGroup).WithMany().HasForeignKey(e => e.ParentGroupId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint("ck_committees_single_parent", "NOT (parent_section_id IS NOT NULL AND parent_group_id IS NOT NULL)"));
            });

            modelBuilder.Entity<UserCommitteeAssignment>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.CommitteeId });
                entity.HasOne(e => e.User)
                    .WithMany(u => u.CommitteeAssignments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Committee)
                    .WithMany(c => c.UserAssignments)
                    .HasForeignKey(e => e.CommitteeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Designation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => new { e.RequiredRole, e.IsActive });
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.HasOne(e => e.FunctionalArea).WithMany(x => x.Designations).HasForeignKey(e => e.FunctionalAreaId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<FunctionalArea>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<OfficeAppointment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.IsActive, e.EffectiveFromUtc, e.EffectiveToUtc });
                entity.HasIndex(e => new { e.DesignationId, e.FunctionalAreaId, e.ScopeType });
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.HasOne(e => e.User).WithMany(x => x.OfficeAppointments).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Designation).WithMany(x => x.OfficeAppointments).HasForeignKey(e => e.DesignationId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.FunctionalArea).WithMany(x => x.OfficeAppointments).HasForeignKey(e => e.FunctionalAreaId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Section).WithMany().HasForeignKey(e => e.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Group).WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Committee).WithMany().HasForeignKey(e => e.CommitteeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.AssignedBy).WithMany().HasForeignKey(e => e.AssignedById).OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_office_appointments_dates", "effective_to_utc IS NULL OR effective_to_utc > effective_from_utc");
                    t.HasCheckConstraint("ck_office_appointments_scope", "(scope_type = 'CONGREGATION' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'FUNCTIONAL_AREA' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'SECTION' AND section_id IS NOT NULL AND group_id IS NULL AND committee_id IS NULL) OR (scope_type = 'GROUP' AND section_id IS NULL AND group_id IS NOT NULL AND committee_id IS NULL) OR (scope_type = 'COMMITTEE' AND section_id IS NULL AND group_id IS NULL AND committee_id IS NOT NULL) OR (scope_type = 'ENTITY' AND ((CASE WHEN section_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN group_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN committee_id IS NOT NULL THEN 1 ELSE 0 END)) = 1)");
                });
            });

            // ---------- REQUISITION ----------
            modelBuilder.Entity<Requisition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RequisitionNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.HasOne(r => r.Group)
                    .WithMany(g => g.Requisitions)
                    .HasForeignKey(r => r.GroupId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(r => r.Section).WithMany().HasForeignKey(r => r.SectionId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(r => r.Committee).WithMany().HasForeignKey(r => r.CommitteeId).OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.Requisitor)
                    .WithMany()
                    .HasForeignKey(r => r.RequisitorId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ApprovedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.ArchivedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ArchivedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(r => r.Items)
                    .WithOne(i => i.Requisition)
                    .HasForeignKey(i => i.RequisitionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- REQUISITION ATTACHMENT ----------
            modelBuilder.Entity<RequisitionAtm>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EntityId);
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne<Requisition>()
                    .WithMany()
                    .HasForeignKey(e => e.EntityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- REQUISITION ITEM ----------
            modelBuilder.Entity<RequisitionItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            // ---------- FUND BALANCE ----------
            modelBuilder.Entity<FundBalance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.GroupId, e.FiscalYear }).IsUnique();
                entity.HasIndex(e => new { e.SectionId, e.FiscalYear }).IsUnique();
                entity.HasIndex(e => new { e.CommitteeId, e.FiscalYear }).IsUnique();
                entity.HasIndex(e => e.FiscalYear)
                    .IsUnique()
                    .HasDatabaseName("ux_fund_balances_organization_fiscal_year")
                    .HasFilter("group_id IS NULL AND section_id IS NULL AND committee_id IS NULL");
                entity.Property(e => e.Version).IsConcurrencyToken();
                
                entity.HasOne(e => e.Group)
                    .WithMany()
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Section)
                    .WithMany()
                    .HasForeignKey(e => e.SectionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Committee)
                    .WithMany()
                    .HasForeignKey(e => e.CommitteeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- SYSTEM CONFIG ----------
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ConfigKey).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Role).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<SmtpSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TemplateKey).IsUnique();
                entity.Property(e => e.HtmlBody).HasColumnType("text");
                entity.Property(e => e.SmsBody).HasColumnType("text");
                entity.Property(e => e.WhatsAppBody).HasColumnType("text");
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TimestampUtc);
                entity.HasIndex(e => new { e.Process, e.TimestampUtc });
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Details).HasColumnType("text");
                entity.Property(e => e.Exception).HasColumnType("text");
            });

            // ---------- CONTRIBUTION ----------
            modelBuilder.Entity<Contribution>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ContributionNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ContributionDate);
                entity.HasIndex(e => e.FinalizedFinancialRecordBookId)
                    .HasFilter("finalized_financial_record_book_id IS NOT NULL");
                entity.HasIndex(e => e.AssetCheckoutId).IsUnique()
                    .HasFilter("asset_checkout_id IS NOT NULL");
                entity.HasIndex(e => new { e.MemberId, e.ContributionDate });
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(c => c.ContributionType)
                    .WithMany(ct => ct.Contributions)
                    .HasForeignKey(c => c.ContributionTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Source)
                    .WithMany(cs => cs.Contributions)
                    .HasForeignKey(c => c.SourceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ReceivedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.ReceivedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.ConfirmedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.ConfirmedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.UpdatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Allocations)
                    .WithOne(a => a.Contribution)
                    .HasForeignKey(a => a.ContributionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<FinancialRecordBook>().WithMany()
                    .HasForeignKey(c => c.FinalizedFinancialRecordBookId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Member).WithMany().HasForeignKey(c => c.MemberId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.PaymentMethodDefinition).WithMany().HasForeignKey(c => c.PaymentMethodDefinitionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.AssetCheckout).WithOne(x => x.RevenueContribution)
                    .HasForeignKey<Contribution>(c => c.AssetCheckoutId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Contribution>().WithMany().HasForeignKey(c => c.ReversesContributionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SystemLicense>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProtectedPayload).HasColumnType("text");
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<LicenseTrustedKey>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProductCode, e.KeyId }).IsUnique();
                entity.HasIndex(e => new { e.ProductCode, e.RotationSequence }).IsUnique();
                entity.Property(e => e.PublicKeyPem).HasColumnType("text");
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<LicenseKeyRotationAud>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PackageHash);
                entity.HasIndex(e => new { e.ProductCode, e.Sequence });
                entity.Property(e => e.FailureReason).HasColumnType("text");
            });

            modelBuilder.Entity<IntegrationSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IntegrationKey).IsUnique();
                entity.Property(e => e.ProtectedSettings).HasColumnType("text");
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<ContributionAud>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ContributionId, e.PerformedAtUtc });
                entity.Property(e => e.ChangesJson).HasColumnType("text");
            });

            modelBuilder.Entity<AssetCategory>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>x.Code).IsUnique(); entity.Property(x=>x.Version).IsConcurrencyToken(); });
            modelBuilder.Entity<Asset>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>x.AssetNumber).IsUnique(); entity.Property(x=>x.Version).IsConcurrencyToken(); entity.HasOne(x=>x.AssetCategory).WithMany().HasForeignKey(x=>x.AssetCategoryId).OnDelete(DeleteBehavior.Restrict); });
            modelBuilder.Entity<AssetCheckout>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>x.RequestNumber).IsUnique(); entity.HasIndex(x=>new{x.AssetId,x.Status}); entity.Property(x=>x.Version).IsConcurrencyToken(); entity.HasOne(x=>x.Asset).WithMany().HasForeignKey(x=>x.AssetId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x=>x.Group).WithMany().HasForeignKey(x=>x.GroupId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x=>x.Section).WithMany().HasForeignKey(x=>x.SectionId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x=>x.Committee).WithMany().HasForeignKey(x=>x.CommitteeId).OnDelete(DeleteBehavior.Restrict); });
            modelBuilder.Entity<AssetMovement>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>new{x.AssetId,x.PerformedAtUtc}); });
            modelBuilder.Entity<AssetAtm>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>x.EntityId); entity.HasOne(x=>x.Asset).WithMany().HasForeignKey(x=>x.EntityId).OnDelete(DeleteBehavior.Cascade); });
            modelBuilder.Entity<AssetCheckoutAtm>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>x.EntityId); entity.HasOne(x=>x.AssetCheckout).WithMany().HasForeignKey(x=>x.EntityId).OnDelete(DeleteBehavior.Cascade); });
            modelBuilder.Entity<AssetLck>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>x.EntityId).IsUnique(); entity.Property(x=>x.Version).IsConcurrencyToken(); entity.HasOne(x=>x.Asset).WithMany().HasForeignKey(x=>x.EntityId).OnDelete(DeleteBehavior.Cascade); });
            modelBuilder.Entity<AssetAud>(entity => { entity.HasKey(x=>x.Id); entity.HasIndex(x=>new{x.AssetId,x.PerformedAtUtc}); entity.Property(x=>x.ChangesJson).HasColumnType("text"); });

            modelBuilder.Entity<RequisitionAud>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.RequisitionId, e.PerformedAtUtc });
                entity.Property(e => e.ChangesJson).HasColumnType("text");
            });

            // ---------- CONTRIBUTION ALLOCATION ----------
            modelBuilder.Entity<ContributionAllocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ContributionId);
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.SectionId);
                entity.HasIndex(e => e.CommitteeId);
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(a => a.Contribution)
                    .WithMany(c => c.Allocations)
                    .HasForeignKey(a => a.ContributionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Group)
                    .WithMany()
                    .HasForeignKey(a => a.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Section).WithMany().HasForeignKey(a => a.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Committee).WithMany().HasForeignKey(a => a.CommitteeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.ContributionType).WithMany().HasForeignKey(a => a.ContributionTypeId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContributionDenomination>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ContributionId, e.Denomination }).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.HasOne(e => e.Contribution)
                    .WithMany(c => c.Denominations)
                    .HasForeignKey(e => e.ContributionId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_contribution_denominations_denomination", "denomination > 0");
                    t.HasCheckConstraint("ck_contribution_denominations_quantity", "quantity > 0");
                    t.HasCheckConstraint("ck_contribution_denominations_amount", "amount > 0");
                });
            });

            // ---------- CONTRIBUTION TYPE ----------
            modelBuilder.Entity<ContributionType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            modelBuilder.Entity<ContributionTypeDestinationPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ContributionTypeId);
                entity.HasIndex(e => e.SectionId);
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.CommitteeId);
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.HasOne(e => e.ContributionType).WithMany(e => e.DestinationPolicies)
                    .HasForeignKey(e => e.ContributionTypeId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Section).WithMany().HasForeignKey(e => e.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Group).WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Committee).WithMany().HasForeignKey(e => e.CommitteeId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_destination_policy_single_destination",
                    "((CASE WHEN section_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN group_id IS NULL THEN 0 ELSE 1 END) + (CASE WHEN committee_id IS NULL THEN 0 ELSE 1 END)) <= 1"));
            });

            // ---------- CONTRIBUTION SOURCE ----------
            modelBuilder.Entity<ContributionSource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            // ---------- BANK ACCOUNT ----------
            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AccountNumber);
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            // ---------- FUND TRANSACTION ----------
            modelBuilder.Entity<FundTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.GroupId);
                entity.HasIndex(e => e.SectionId);
                entity.HasIndex(e => e.CommitteeId);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.PostingKey).IsUnique().HasFilter("posting_key IS NOT NULL");
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(t => t.Group)
                    .WithMany()
                    .HasForeignKey(t => t.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Section).WithMany().HasForeignKey(t => t.SectionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Committee).WithMany().HasForeignKey(t => t.CommitteeId).OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.UpdatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- CONTRIBUTION ATTACHMENT (ATM) ----------
            modelBuilder.Entity<ContributionAtm>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EntityId);
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(a => a.Contribution)
                    .WithMany(c => c.Attachments)
                    .HasForeignKey(a => a.EntityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- CONTRIBUTION LOCK (LCK) ----------
            modelBuilder.Entity<ContributionLck>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EntityId).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(l => l.Contribution)
                    .WithMany(c => c.Locks)
                    .HasForeignKey(l => l.EntityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== NEW: REQUISITION LOCK =====
            // No .ToTable() – let EF Core use default naming (requisition_lcks)
            modelBuilder.Entity<RequisitionLck>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EntityId).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            // ===== NEW: USER LOCK =====
            // No .ToTable() – let EF Core use default naming (user_lcks)
            modelBuilder.Entity<UserLck>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EntityId).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            // ---------- ACCOUNTING INTEGRATION ----------
            // The integration layer stages balanced accounting batches from the
            // authoritative eMilimo contribution register. It deliberately does
            // not alter contribution, service-collection, or ledger workflows.
            modelBuilder.Entity<AccountingIntegrationProfile>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.ProviderKey, x.CompanyIdentifier }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.IntegrationSetting).WithMany()
                    .HasForeignKey(x => x.IntegrationSettingId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AccountingAccountMapping>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.AccountingIntegrationProfileId, x.MappingType, x.SourceKey }).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.AccountingIntegrationProfile).WithMany(x => x.Mappings)
                    .HasForeignKey(x => x.AccountingIntegrationProfileId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AccountingExternalCode>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.AccountingIntegrationProfileId, x.CodeType, x.Code }).IsUnique();
                entity.HasIndex(x => new { x.AccountingIntegrationProfileId, x.CodeType, x.IsActive });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.AccountingIntegrationProfile).WithMany()
                    .HasForeignKey(x => x.AccountingIntegrationProfileId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AccountingBatch>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.BatchNumber).IsUnique();
                entity.HasIndex(x => new { x.AccountingIntegrationProfileId, x.SourceHash })
                    .IsUnique().HasFilter("status <> 'CANCELLED'");
                entity.HasIndex(x => new { x.Status, x.CreatedDate });
                entity.HasIndex(x => new { x.AccountingIntegrationProfileId, x.ImportStatus });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.AccountingIntegrationProfile).WithMany()
                    .HasForeignKey(x => x.AccountingIntegrationProfileId).OnDelete(DeleteBehavior.Restrict);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("ck_accounting_batch_period", "period_end >= period_start");
                    t.HasCheckConstraint("ck_accounting_batch_totals", "debit_total >= 0 AND credit_total >= 0");
                    t.HasCheckConstraint("ck_accounting_batch_import_counts", "accepted_line_count >= 0 AND rejected_line_count >= 0");
                });
            });

            modelBuilder.Entity<AccountingBatchLine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.AccountingBatchId, x.LineNumber }).IsUnique();
                entity.HasIndex(x => new { x.SourceType, x.SourceId });
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne(x => x.AccountingBatch).WithMany(x => x.Lines)
                    .HasForeignKey(x => x.AccountingBatchId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint(
                    "ck_accounting_batch_line_one_side",
                    "(debit > 0 AND credit = 0) OR (credit > 0 AND debit = 0)"));
            });

            ConfigureAccountingCompanions<AccountingIntegrationProfileAtm, AccountingIntegrationProfileLck, AccountingIntegrationProfileAud, AccountingIntegrationProfile>(modelBuilder);
            ConfigureAccountingCompanions<AccountingAccountMappingAtm, AccountingAccountMappingLck, AccountingAccountMappingAud, AccountingAccountMapping>(modelBuilder);
            ConfigureAccountingCompanions<AccountingExternalCodeAtm, AccountingExternalCodeLck, AccountingExternalCodeAud, AccountingExternalCode>(modelBuilder);
            ConfigureAccountingCompanions<AccountingBatchAtm, AccountingBatchLck, AccountingBatchAud, AccountingBatch>(modelBuilder);
            ConfigureAccountingCompanions<AccountingBatchLineAtm, AccountingBatchLineLck, AccountingBatchLineAud, AccountingBatchLine>(modelBuilder);
        }

        private static void ConfigureAccountingCompanions<TAtm, TLck, TAud, TEntity>(ModelBuilder modelBuilder)
            where TAtm : AttachmentCompanionBase
            where TLck : LockCompanionBase
            where TAud : AccountingAuditCompanionBase
            where TEntity : class
        {
            modelBuilder.Entity<TAtm>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.EntityId);
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne<TEntity>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<TLck>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.EntityId).IsUnique();
                entity.Property(x => x.Version).IsConcurrencyToken();
                entity.HasOne<TEntity>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.LockedByUser).WithMany().HasForeignKey(x => x.LockedById).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<TAud>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.EntityId, x.PerformedAtUtc });
                entity.Property(x => x.ChangesJson).HasColumnType("jsonb");
                entity.HasOne<TEntity>().WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddProcessAuditEntries();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            AddProcessAuditEntries();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void AddProcessAuditEntries()
        {
            ChangeTracker.DetectChanges();

            foreach (var entry in ChangeTracker.Entries<Contribution>()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                         .ToList())
            {
                ContributionAudits.Add(new ContributionAud
                {
                    Id = Guid.NewGuid(),
                    ContributionId = entry.Entity.Id,
                    Action = AuditAction(entry),
                    ChangesJson = SerializeChanges(entry),
                    PerformedById = entry.Entity.UpdatedById ?? entry.Entity.CreatedById,
                    PerformedAtUtc = DateTime.UtcNow
                });
            }

            foreach (var entry in ChangeTracker.Entries<Requisition>()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                         .ToList())
            {
                RequisitionAudits.Add(new RequisitionAud
                {
                    Id = Guid.NewGuid(),
                    RequisitionId = entry.Entity.Id,
                    Action = AuditAction(entry),
                    ChangesJson = SerializeChanges(entry),
                    PerformedById = entry.Entity.UpdatedById ?? entry.Entity.CreatedById ?? entry.Entity.RequisitorId,
                    PerformedAtUtc = DateTime.UtcNow
                });
            }

            foreach (var entry in ChangeTracker.Entries<Asset>()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
                AssetAudits.Add(new AssetAud { Id=Guid.NewGuid(),AssetId=entry.Entity.Id,RecordType="ASSET",Action=AuditAction(entry),ChangesJson=SerializeChanges(entry),PerformedById=entry.Entity.UpdatedById??entry.Entity.CreatedById,PerformedAtUtc=DateTime.UtcNow });

            foreach (var entry in ChangeTracker.Entries<AssetCheckout>()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
                AssetAudits.Add(new AssetAud { Id=Guid.NewGuid(),AssetId=entry.Entity.AssetId,AssetCheckoutId=entry.Entity.Id,RecordType="CHECKOUT",Action=AuditAction(entry),ChangesJson=SerializeChanges(entry),PerformedById=entry.Entity.UpdatedById??entry.Entity.CreatedById??entry.Entity.RequestedById,PerformedAtUtc=DateTime.UtcNow });

            foreach (var entry in ChangeTracker.Entries<FinancialRecordBook>()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
                FrbAudits.Add(new FrbAud { Id=Guid.NewGuid(),FinancialRecordBookId=entry.Entity.Id,Action=AuditAction(entry),ChangesJson=SerializeChanges(entry),PerformedById=entry.Entity.UpdatedById??entry.Entity.CreatedById,PerformedAtUtc=DateTime.UtcNow });

            AddAccountingAudits<AccountingIntegrationProfile, AccountingIntegrationProfileAud>(AccountingIntegrationProfileAudits);
            AddAccountingAudits<AccountingAccountMapping, AccountingAccountMappingAud>(AccountingAccountMappingAudits);
            AddAccountingAudits<AccountingExternalCode, AccountingExternalCodeAud>(AccountingExternalCodeAudits);
            AddAccountingAudits<AccountingBatch, AccountingBatchAud>(AccountingBatchAudits);
            AddAccountingAudits<AccountingBatchLine, AccountingBatchLineAud>(AccountingBatchLineAudits);
        }

        private void AddAccountingAudits<TEntity, TAudit>(DbSet<TAudit> audits)
            where TEntity : eMilimo.Core.Entities.Base.BaseEntity
            where TAudit : AccountingAuditCompanionBase, new()
        {
            foreach (var entry in ChangeTracker.Entries<TEntity>()
                         .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                         .ToList())
            {
                audits.Add(new TAudit
                {
                    Id = Guid.NewGuid(),
                    EntityId = entry.Entity.Id,
                    Action = AuditAction(entry),
                    ChangesJson = SerializeChanges(entry),
                    PerformedById = entry.Entity.UpdatedById ?? entry.Entity.CreatedById,
                    PerformedAtUtc = DateTime.UtcNow
                });
            }
        }

        private static string AuditAction<TEntity>(EntityEntry<TEntity> entry) where TEntity : class
        {
            if (entry.State == EntityState.Added) return "CREATED";
            if (entry.State == EntityState.Deleted) return "DELETED";

            var status = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Status");
            return status?.IsModified == true
                ? $"STATUS_{status.CurrentValue?.ToString()?.ToUpperInvariant()}"
                : "UPDATED";
        }

        private static string SerializeChanges<TEntity>(EntityEntry<TEntity> entry) where TEntity : class
        {
            var changes = entry.Properties
                .Where(p => entry.State != EntityState.Modified || p.IsModified)
                .ToDictionary(
                    p => p.Metadata.Name,
                    p => new
                    {
                        Before = entry.State == EntityState.Added ? null : p.OriginalValue,
                        After = entry.State == EntityState.Deleted ? null : p.CurrentValue
                    });

            return JsonSerializer.Serialize(changes);
        }
    }
}
