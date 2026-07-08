using UCP.Core.Entities.Companions;
using Microsoft.EntityFrameworkCore;
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
        public DbSet<FundBalance> FundBalances { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---------- USER ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.MembershipId).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();

                // Self‑referencing relationships (CreatedBy, UpdatedBy)
                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationships to Section and Group
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

                // Leader relationship (User)
                entity.HasOne(g => g.Leader)
                    .WithMany()
                    .HasForeignKey(g => g.LeaderId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Parent/Child self‑reference
                entity.HasOne(g => g.ParentGroup)
                    .WithMany(g => g.ChildGroups)
                    .HasForeignKey(g => g.ParentGroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- REQUISITION ----------
            modelBuilder.Entity<Requisition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RequisitionNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.Version).IsConcurrencyToken();

                entity.HasOne(r => r.Group)
                    .WithMany(g => g.Requisitions)
                    .HasForeignKey(r => r.GroupId)
                    .OnDelete(DeleteBehavior.SetNull);

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

                // Relationship to Requisition is already configured above
            });

            // ---------- FUND BALANCE ----------
            modelBuilder.Entity<FundBalance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.GroupId, e.FiscalYear }).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
                entity.HasOne(e => e.Group)
                    .WithMany()
                    .HasForeignKey(e => e.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- SYSTEM CONFIG ----------
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ConfigKey).IsUnique();
                entity.Property(e => e.Version).IsConcurrencyToken();
            });
        }
    }
}