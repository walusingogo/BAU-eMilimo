using System;
using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261101210000_AddAccountingImportReconciliation")]
public partial class AddAccountingImportReconciliation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "accepted_line_count", table: "accounting_batches", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "import_file_name", table: "accounting_batches", type: "character varying(260)", maxLength: 260, nullable: true);
        migrationBuilder.AddColumn<string>(name: "import_message", table: "accounting_batches", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "import_status", table: "accounting_batches", type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "NOT_RECEIVED");
        migrationBuilder.AddColumn<DateTime>(name: "imported_at_utc", table: "accounting_batches", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "imported_by_id", table: "accounting_batches", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<int>(name: "rejected_line_count", table: "accounting_batches", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.CreateIndex(name: "ix_accounting_batches_accounting_integration_profile_id_import", table: "accounting_batches", columns: new[] { "accounting_integration_profile_id", "import_status" });
        migrationBuilder.AddCheckConstraint(name: "ck_accounting_batch_import_counts", table: "accounting_batches", sql: "accepted_line_count >= 0 AND rejected_line_count >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "ck_accounting_batch_import_counts", table: "accounting_batches");
        migrationBuilder.DropIndex(name: "ix_accounting_batches_accounting_integration_profile_id_import", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "accepted_line_count", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "import_file_name", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "import_message", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "import_status", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "imported_at_utc", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "imported_by_id", table: "accounting_batches");
        migrationBuilder.DropColumn(name: "rejected_line_count", table: "accounting_batches");
    }
}
