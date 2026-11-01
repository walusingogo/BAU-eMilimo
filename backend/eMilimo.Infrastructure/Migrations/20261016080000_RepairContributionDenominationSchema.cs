using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261016080000_RepairContributionDenominationSchema")]
public partial class RepairContributionDenominationSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // AddContributionCashDenominations intentionally used CREATE TABLE IF NOT EXISTS
        // so it could adopt an older table. An interrupted legacy deployment can therefore
        // leave the table present but without this BaseEntity column while EF records the
        // original migration as applied. Repair that partial schema idempotently.
        migrationBuilder.Sql("""
            ALTER TABLE contribution_denominations
            ADD COLUMN IF NOT EXISTS last_processed_time_stamp timestamp with time zone NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately irreversible: the column is part of the current entity contract and
        // may contain processing/audit state written after this repair is deployed.
    }
}
