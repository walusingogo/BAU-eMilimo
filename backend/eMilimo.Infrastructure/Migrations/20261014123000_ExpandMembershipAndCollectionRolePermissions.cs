using eMilimo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261014123000_ExpandMembershipAndCollectionRolePermissions")]
public partial class ExpandMembershipAndCollectionRolePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing installations have persisted role rows. Add the v1 defaults for
        // the new modules without replacing any administrator-defined permissions.
        migrationBuilder.Sql("""
            UPDATE role_permissions
            SET permissions_json = (
                    permissions_json::jsonb ||
                    '["members.view","members.manage","members.financial-activity","membership.sync.manage","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit"]'::jsonb
                )::text,
                updated_date = CURRENT_TIMESTAMP,
                version = version + 1
            WHERE role = 'Admin';

            UPDATE role_permissions
            SET permissions_json = (
                    permissions_json::jsonb ||
                    '["members.view","service-collections.view","service-collections.create","service-collections.edit","service-collections.submit"]'::jsonb
                )::text,
                updated_date = CURRENT_TIMESTAMP,
                version = version + 1
            WHERE role IN ('SeniorRep', 'GroupRep');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally retained: access may have been adjusted by an administrator
        // after the upgrade, so a downgrade must not silently revoke it.
    }
}
