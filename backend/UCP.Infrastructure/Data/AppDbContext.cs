// backend/UCP.Infrastructure/Data/AppDbContext.cs
using UCP.Core.Entities.Companions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using UCP.Core.Entities;

namespace UCP.Infrastructure.Data
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
        public DbSet<SystemLog> Logs { get; set; }
        public DbSet<RequisitionLck> RequisitionLcks { get; set; }
        public DbSet<UserLck> UserLcks { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<SmtpSetting> SmtpSettings { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<CommunicationLog> CommunicationLogs { get; set; }
        public DbSet<SystemLicense> SystemLicenses { get; set; }
        public DbSet<LicenseTrustedKey> LicenseTrustedKeys { get; set; }
        public DbSet<LicenseKeyRotationAud> LicenseKeyRotationAudits { get; set; }
        public DbSet<IntegrationSetting> IntegrationSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommunicationLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.SentAtUtc);
                entity.HasIndex(x => new { x.Channel, x.Status, x.SentAtUtc });
                entity.HasIndex(x => x.RetryOfCommunicationLogId);
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

            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SentAtUtc);
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.HtmlBody).HasColumnType("text");
                entity.HasIndex(e => e.RetryOfEmailLogId);
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

                entity.HasOne(a => a.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- CONTRIBUTION TYPE ----------
            modelBuilder.Entity<ContributionType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
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
