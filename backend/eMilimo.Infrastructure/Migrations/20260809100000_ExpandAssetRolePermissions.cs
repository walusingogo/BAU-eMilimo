using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using eMilimo.Infrastructure.Data;

#nullable disable

namespace eMilimo.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260809100000_ExpandAssetRolePermissions")]
public partial class ExpandAssetRolePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Preserve the access users had before checkout requests and asset reports
        // became independently assignable processes.
        migrationBuilder.Sql("""
            UPDATE role_permissions
            SET permissions_json = (permissions_json::jsonb || '["assets.request"]'::jsonb)::text
            WHERE role <> 'Auditor'
              AND permissions_json::jsonb ? 'assets.view'
              AND NOT permissions_json::jsonb ? 'assets.request';

            UPDATE role_permissions
            SET permissions_json = (permissions_json::jsonb || '["assets.reports"]'::jsonb)::text
            WHERE permissions_json::jsonb ? 'reports.view'
              AND NOT permissions_json::jsonb ? 'assets.reports';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally retained: these may have been explicitly changed by an administrator.
    }
}
