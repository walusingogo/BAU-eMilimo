using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261031100000_BackfillContributionDestinationPolicies")]
public partial class BackfillContributionDestinationPolicies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Older allocation rows did not store their own contribution type. Use
        // the parent contribution's type to infer only historically-used scopes.
        migrationBuilder.Sql(
            """
            UPDATE contribution_types AS ct
            SET allow_organization_wide_allocation = ct.allow_organization_wide_allocation OR EXISTS (
                    SELECT 1
                    FROM contribution_allocations AS ca
                    JOIN contributions AS c ON c.id = ca.contribution_id
                    WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
                      AND ca.section_id IS NULL
                      AND ca.group_id IS NULL
                      AND ca.committee_id IS NULL),
                allow_section_allocation = ct.allow_section_allocation OR EXISTS (
                    SELECT 1
                    FROM contribution_allocations AS ca
                    JOIN contributions AS c ON c.id = ca.contribution_id
                    WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
                      AND ca.section_id IS NOT NULL),
                allow_group_allocation = ct.allow_group_allocation OR EXISTS (
                    SELECT 1
                    FROM contribution_allocations AS ca
                    JOIN contributions AS c ON c.id = ca.contribution_id
                    WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
                      AND ca.group_id IS NOT NULL),
                allow_committee_allocation = ct.allow_committee_allocation OR EXISTS (
                    SELECT 1
                    FROM contribution_allocations AS ca
                    JOIN contributions AS c ON c.id = ca.contribution_id
                    WHERE COALESCE(ca.contribution_type_id, c.contribution_type_id) = ct.id
                      AND ca.committee_id IS NOT NULL);

            -- Tithe is explicitly an organization-wide basket in the approved
            -- requirements. Other new policies remain administrator-controlled.
            UPDATE contribution_types
            SET allow_organization_wide_allocation = TRUE,
                default_digital_destination_type = COALESCE(default_digital_destination_type, 'ORGANIZATION')
            WHERE upper(code) = 'TITHE';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Historical eligibility cannot be safely distinguished from later
        // administrator choices, so this data backfill is intentionally retained.
    }
}
