using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030150000_RepairContributionDestinationDefaults")]
public partial class RepairContributionDestinationDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve only destination patterns evidenced by existing allocations.
        // This keeps established workflows operational without granting broader
        // financial-routing permissions to every contribution type.
        migrationBuilder.Sql(
            """
            UPDATE contribution_types AS ct
            SET allow_organization_wide_allocation = EXISTS (
                    SELECT 1 FROM contribution_allocations AS ca
                    WHERE ca.contribution_type_id = ct.id
                      AND ca.section_id IS NULL
                      AND ca.group_id IS NULL
                      AND ca.committee_id IS NULL),
                allow_section_allocation = EXISTS (
                    SELECT 1 FROM contribution_allocations AS ca
                    WHERE ca.contribution_type_id = ct.id AND ca.section_id IS NOT NULL),
                allow_group_allocation = EXISTS (
                    SELECT 1 FROM contribution_allocations AS ca
                    WHERE ca.contribution_type_id = ct.id AND ca.group_id IS NOT NULL),
                allow_committee_allocation = EXISTS (
                    SELECT 1 FROM contribution_allocations AS ca
                    WHERE ca.contribution_type_id = ct.id AND ca.committee_id IS NOT NULL)
            WHERE ct.allow_organization_wide_allocation = FALSE
              AND ct.allow_section_allocation = FALSE
              AND ct.allow_group_allocation = FALSE
              AND ct.allow_committee_allocation = FALSE;
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS ux_fund_balances_organization_fiscal_year
            ON fund_balances (fiscal_year)
            WHERE group_id IS NULL AND section_id IS NULL AND committee_id IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS ux_fund_balances_organization_fiscal_year;");
    }
}
