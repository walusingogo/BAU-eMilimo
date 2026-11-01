using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowCancelledAccountingBatchRegeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_accounting_batches_accounting_integration_profile_id_source",
                table: "accounting_batches");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batches_accounting_integration_profile_id_source",
                table: "accounting_batches",
                columns: new[] { "accounting_integration_profile_id", "source_hash" },
                unique: true,
                filter: "status <> 'CANCELLED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_accounting_batches_accounting_integration_profile_id_source",
                table: "accounting_batches");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_batches_accounting_integration_profile_id_source",
                table: "accounting_batches",
                columns: new[] { "accounting_integration_profile_id", "source_hash" },
                unique: true);
        }
    }
}
